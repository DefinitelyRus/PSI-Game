using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;

namespace CommonScripts;

/// <summary>
/// Represents a context associated with the current thread.
/// </summary>
/// <remarks>
/// This class provides a mechanism to manage and track the state of a context tied to the thread that created it.
/// Once the context is ended using the <see cref="End"/> method, it is considered inactive.
/// </remarks>
[GlobalClass]
public partial class Context : GodotObject {
	internal readonly int ThreadId;
	internal bool Active = true;

	public Context() {
		ThreadId = System.Environment.CurrentManagedThreadId;
	}

	public enum Mode {
		Message,
		Warning,
		Error,
		Expired
	}


	/// <summary>
	/// Logs a trace message with contextual information, including the call stack, to the Godot console.
	/// </summary>
	/// <remarks>
	/// This method captures the current call stack, filters out irrelevant frames
	/// (e.g., from system or third-party namespaces), and logs the trace information to the Godot console.
	/// The message is logged at the deepest relevant stack frame, with intermediate frames logged as context.
	/// </remarks>
	/// <param name="message">The message to log. This will be displayed at the deepest stack frame.</param>
	/// <param name="printAs">Specifies the severity level of the message, such as normal, warning, or error.</param>
	/// <param name="frameDepth">The number of stack frames to skip when capturing the call stack. Defaults to 1.</param>
	/// <param name="filePath">The source file path of the caller. Automatically provided by the compiler.</param>
	/// <param name="line">The line number in the source file of the caller. Automatically provided by the compiler.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the trace context is not active, or if it belongs to a different thread.
	/// Thrown if no stack frames are available for trace logging.
	/// </exception>
	public void Message(string message, Mode printAs, int frameDepth = 1, bool skipTrace = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0) {
		if (!Active) printAs = Mode.Expired; // Allow logging in inactive context, but mark as error.

		if (ThreadId != System.Environment.CurrentManagedThreadId) throw new InvalidOperationException("This trace context belongs to a different thread.");

		// Capture the current stack (except this function).
		StackTrace trace = new(frameDepth, true);
		StackFrame[]? frames = trace.GetFrames();
		if (frames == null || frames.Length == 0) throw new InvalidOperationException("No stack frames available for trace logging.");

		// Removes frames from System, Microsoft, and Godot namespaces.
		var relevantFrames = frames.Where(f => {
			MethodBase? method = f.GetMethod();
			Type? type = method?.DeclaringType;

			if (type == null) return false;

			string? @namespace = type.Namespace;
			if (string.IsNullOrEmpty(@namespace)) return true;

			bool isSystem = @namespace.StartsWith("System");
			bool isMicrosoft = @namespace.StartsWith("Microsoft");
			bool isGodot = @namespace.StartsWith("Godot");
			bool isInvoked = method!.Name == "InvokeGodotClassMethod";
			return !isSystem && !isMicrosoft && !isGodot && !isInvoked;
		})
		.Reverse()
		.ToArray();

		// Determine the current depth based on relevant frames.
		int depth = 0;
		foreach (var frame in relevantFrames) {
			MethodBase? method = frame.GetMethod();

			string indent = new(' ', depth);
			string prefix;

			// Attempt getting the class and method name from the stack trace.
			if (method != null) {
				string className = method.DeclaringType?.Name ?? string.Empty;
				string methodName = method.Name;
				int frameLine = frame.GetFileLineNumber();
				indent = new(' ', depth);
				indent = (depth == 0 ? "\n" : "") + indent;
				prefix = frameLine > 0
					? $"{indent}[{className}.{methodName}:{frameLine}]"
					: $"{indent}[{className}.{methodName}:?]";
			}

			// Use the file path and line number.
			else {
				string fileName = Path.GetFileName(filePath);
				prefix = $"{indent}[{fileName} @ line {line}]";
			}

			// Print message only at the last frame.
			if (depth == relevantFrames.Length - 1) {
				string insert = printAs switch {
					Mode.Warning => "WARN: ",
					Mode.Error => "ERROR: ",
					Mode.Expired => "EXPIRED: ",
					_ => string.Empty
				};

				string loggedMessage = $"{prefix} {insert}{message}";

				switch (printAs) {
					case Mode.Message:
						GD.Print(loggedMessage);
						break;
					case Mode.Warning:
						GD.Print(loggedMessage);
						GD.PushWarning(loggedMessage);
						break;
					case Mode.Error:
					case Mode.Expired:
						GD.Print(loggedMessage);
						GD.PushError(loggedMessage);
						break;
				}
			}

			// Print only the prefix for intermediate frames.
			else if (!skipTrace) GD.Print(prefix);

			depth++;
			continue;
		}
	}


	/// <summary>
	/// Logs the message and traces where the method was called from.
	/// </summary>
	/// <param name="message">The message to log. If <see langword="null"/>, an empty string is used.</param>
	public void Trace(string? message, bool enabled = true) {
		if (!enabled) return;
		Message(message ?? string.Empty, Mode.Message, 2);
	}


	/// <summary>
	/// Logs the message and traces where the method was called from.
	/// </summary>
	/// <remarks>
	/// This method is intended for scenarios where the trace message construction is expensive,
	/// as the <paramref name="messageFactory"/> delegate is only evaluated if tracing is enabled.
	/// </remarks>
	/// <param name="messageFactory">
	/// A delegate that generates the trace message.
	/// The delegate is invoked only if tracing is enabled.
	/// </param>
	public void Trace(Func<string> messageFactory, bool enabled = true) {
		if (!enabled) return;
		Message(messageFactory(), Mode.Message, 2);
	}


	/// <summary>
	/// Logs the message without tracing where the method was called from.
	/// </summary>
	/// <param name="message">
	/// The message to add.
	/// If <paramref name="message"/> is <see langword="null"/>, an empty string is used.
	/// </param>
	/// <param name="enabled">
	/// A value indicating whether the operation is enabled.
	/// If <see langword="false"/>, the method performs no action.
	/// Defaults to <see langword="true"/>.
	/// </param>
	public void Log(string? message, bool enabled = true) {
		if (!enabled) return;
		Message(message ?? string.Empty, Mode.Message, 2, true);
	}


	/// <summary>
	/// Logs the message without tracing where the method was called from.
	/// </summary>
	/// <remarks>
	/// This method allows deferred message generation by accepting a <see cref="Func{TResult}"/> delegate,
	/// which is only executed if the <paramref name="enabled"/> parameter is <see langword="true"/>.
	/// </remarks>
	/// <param name="messageFactory">
	/// A delegate that generates the message to be logged.
	/// The delegate is only invoked if logging is enabled.
	/// </param>
	/// <param name="enabled">
	/// A value indicating whether the message should be logged.
	/// The default value is <see langword="true"/>.
	/// </param>
	public void Log(Func<string> messageFactory, bool enabled = true) {
		if (!enabled) return;
		Message(messageFactory(), Mode.Message, 2, true);
	}


	/// <summary>
	/// Logs the warning message and traces where the method was called from.
	/// </summary>
	/// <param name="message">
	/// The warning message to log.
	/// If <paramref name="message"/> is <see langword="null"/>, an empty string is logged.
	/// </param>
	public void Warn(string? message, bool enabled = true) {
		if (!enabled) return;
		Message(message ?? string.Empty, Mode.Warning, 2);
	}


	/// <summary>
	/// Logs the warning message and traces where the method was called from.
	/// </summary>
	/// <remarks>
	/// This method is intended for scenarios where the trace message construction is expensive,
	/// as the <paramref name="messageFactory"/> delegate is only evaluated if tracing is enabled.
	/// </remarks>
	/// <param name="messageFactory">
	/// A delegate that generates the warning message to be logged.
	/// The delegate is invoked only if tracing is enabled.
	/// </param>
	public void Warn(Func<string> messageFactory, bool enabled = true) {
		if (!enabled) return;
		Message(messageFactory(), Mode.Warning, 2);
	}


	/// <summary>
	/// Logs the error message and traces where the method was called from.
	/// </summary>
	/// <param name="message">
	/// The error message to log.
	/// If <see langword="null"/>, an empty string is logged.
	/// </param>
	public void Err(string? message, bool enabled = true) {
		if (!enabled) return;
		Message(message ?? string.Empty, Mode.Error, 2);
	}


	/// <summary>
	/// Logs the error message and traces where the method was called from.
	/// </summary>
	/// <remarks>
	/// This method is intended for scenarios where the trace message construction is expensive,
	/// as the <paramref name="messageFactory"/> delegate is only evaluated if tracing is enabled.
	/// </remarks>
	public void Err(Func<string> messageFactory, bool enabled = true) {
		if (!enabled) return;
		Message(messageFactory(), Mode.Error, 2);
	}


	/// <summary>
	/// Marks the end of the current operation or process.
	/// </summary>
	/// <remarks>
	/// This method sets the <see cref="Active"/> property to <see langword="false"/>,
	/// indicating that the operation is no longer active.
	/// </remarks>
	public void End() {
		Active = false;
	}


	public static int GetDepth() {
		StackTrace trace = new(1, true);
		StackFrame[]? frames = trace.GetFrames();
		if (frames == null || frames.Length == 0) return 0;

		// Removes frames from System, Microsoft, and Godot namespaces.
		var relevantFrames = frames.Where(f => {
			MethodBase? method = f.GetMethod();
			Type? type = method?.DeclaringType;
			if (type == null) return false;
			string? @namespace = type.Namespace;
			if (string.IsNullOrEmpty(@namespace)) return true;
			// Exclude System, Microsoft and Godot namespaces.
			return !@namespace.StartsWith("System") && !@namespace.StartsWith("Microsoft") && !@namespace.StartsWith("Godot");
		})
		.Reverse()
		.ToArray();

		return relevantFrames.Length;
	}
}