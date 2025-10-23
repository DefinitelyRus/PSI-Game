global using Ctx = CommonScripts.Context;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
namespace CommonScripts;

public static class Log {

    public enum PrintMode {
        Message,
        Warning,
		Error,
        FileOnly
	}


	/// <summary>
	/// Logs a message to the console with additional context information.
	/// </summary>
	/// <param name="message">
	/// The message to log.
	/// </param>
	/// <param name="printAs">
	/// Whether to print the message as a regular message, warning, or error, or if the message should only be printed to file.
	/// </param>
	/// <param name="stack">
	/// Basically how many spaces to indent the message.
	/// Used for improving readaibility in verbose step-by-step logs.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	/// <param name="frame">
	/// How many stack layers deep this function is called from.
	/// Change this if you are calling this function from a helper function.
	/// </param>
	/// <param name="filePath">
	/// This is auto-filled on compile time.
	/// It's the entire folder structure that contains the file that called this funciton.
	/// </param>
	/// <param name="line">
	/// This is auto-filled on compile time.
	/// It's the line number from the source code file.
	/// </param>
	public static string Message(string message, PrintMode printAs = PrintMode.Message, int stack = 0, bool useFilePath = false, int frame = 1, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0
    ) {
        string prefix;
        string padding = new(' ', stack);

        MethodBase? method = new StackTrace(frame, false).GetFrame(0)?.GetMethod();

        // Attempt getting the class and method name from the stack trace.
        if (method != null && !useFilePath) {
            string className = method.DeclaringType?.Name ?? string.Empty;
            string methodName = method.Name;
            prefix = $"{padding}[{className}.{methodName}]";
        }

        // Use the file path, method name, and line number.
        else {
            string fileName = Path.GetFileName(filePath);
            prefix = $"{padding}[{fileName} @ line {line}]";
        }

		// Insert prefix based on print mode.
		string insert = string.Empty;
		if (printAs == PrintMode.Warning) insert = "WARN: ";
        else if (printAs == PrintMode.Error) insert = "ERROR: ";

		string loggedMessage = $"{prefix} {insert}{message}";

        // Print as regular message.
        if (printAs == PrintMode.Message) {
            GD.Print(loggedMessage); //TODO: Replace with a proper logging framework.
        }

		// Print as warning and push to warning stack.
		else if (printAs == PrintMode.Warning) {
			GD.Print(loggedMessage);
			GD.PushWarning(loggedMessage);
        }

		// Print as error and push to error stack.
		else if (printAs == PrintMode.Error) {
			GD.Print(loggedMessage);
			GD.PushError(loggedMessage);
        }

		return loggedMessage;
	}


	/// <summary>
	/// Logs a message to the console with additional context information.
	/// </summary>
	/// <param name="message">
	/// The message to log.
	/// </param>
	/// <param name="enabled">
	/// Whether to print the message at all.
	/// If you want to log a message to file, use <see cref="Message"/> instead.
	/// </param>
	/// <param name="stack">
	/// How deep this function is in the stack trace.
	/// Basically, all this does is indent the message by this number of spaces.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	public static void Me(string message, bool enabled = true, int stack = 0, bool useFilePath = false) {
        if (!enabled) return;
        if (enabled == false) return;
		Message(message, PrintMode.Message, stack, useFilePath, 2);
    }


	/// <summary>
	/// Logs a message to the console with additional context information. <br/>
	/// This is best used for logging messages that don't always need to be logged,
	/// saving computation time when the message <c>enabled = false</c>.
	/// </summary>
	/// <param name="messageFactory">
	/// The message to log, as a function that returns a string.
	/// </param>
	/// <param name="enabled">
	/// Whether to print the message at all.
	/// If you want to log a message to file, use <see cref="Message"/> instead.
	/// </param>
	/// <param name="stack">
	/// How deep this function is in the stack trace.
	/// Basically, all this does is indent the message by this number of spaces.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	public static void Me(Func<string> messageFactory, bool enabled = true, int stack = 0, bool useFilePath = false) {
        if (!enabled) return;
        Message(messageFactory(), PrintMode.Message, stack, useFilePath, 2);
    }


	/// <summary>
	/// Logs a warning message to the console with additional context information.
	/// </summary>
	/// <param name="message">
	/// The message to log.
	/// </param>
	/// <param name="enabled">
	/// Whether to print the message at all.
	/// If you want to log a message to file, use <see cref="Message"/> instead.
	/// </param>
	/// <param name="stack">
	/// How deep this function is in the stack trace.
	/// Basically, all this does is indent the message by this number of spaces.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	public static void Warn(string message, bool enabled = true, int stack = 0, bool useFilePath = false) {
        if (!enabled) return;
        Message(message, PrintMode.Warning, stack, useFilePath, 2);
	}


	/// <summary>
	/// Logs a warning message to the console with additional context information.
	/// </summary>
	/// <param name="messageFactory">
	/// The message to log, as a function that returns a string.
	/// </param>
	/// <param name="enabled">
	/// Whether to print the message at all.
	/// If you want to log a message to file, use <see cref="Message"/> instead.
	/// </param>
	/// <param name="stack">
	/// How deep this function is in the stack trace.
	/// Basically, all this does is indent the message by this number of spaces.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	public static void Warn(Func<string> messageFactory, bool enabled = true, int stack = 0, bool useFilePath = false) {
		if (!enabled) return;
		Message(messageFactory(), PrintMode.Warning, stack, useFilePath, 2);
	}


	/// <summary>
	/// Logs an error message to the console with additional context information.
	/// </summary>
	/// <param name="message">
	/// The message to log.
	/// </param>
	/// <param name="enabled">
	/// Whether to print the message at all.
	/// If you want to log a message to file, use <see cref="Message"/> instead.
	/// </param>
	/// <param name="stack">
	/// How deep this function is in the stack trace.
	/// Basically, all this does is indent the message by this number of spaces.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	public static void Err(string message, bool enabled = true, int stack = 0, bool useFilePath = false) {
        if (!enabled) return;
        Message(message, PrintMode.Error, stack, useFilePath, 2);
    }


	/// <summary>
	/// Logs an error message to the console with additional context information.
	/// </summary>
	/// <param name="messageFactory">
	/// The message to log, as a function that returns a string.
	/// </param>
	/// <param name="enabled">
	/// Whether to print the message at all.
	/// If you want to log a message to file, use <see cref="Message"/> instead.
	/// </param>
	/// <param name="stack">
	/// How deep this function is in the stack trace.
	/// Basically, all this does is indent the message by this number of spaces.
	/// </param>
	/// <param name="useFilePath">
	/// Whether to use the file path and line number for logging instead of the class and method names.
	/// </param>
	public static void Err(Func<string> messageFactory, bool enabled = true, int stack = 0, bool useFilePath = false) {
		if (!enabled) return;
		Message(messageFactory(), PrintMode.Error, stack, useFilePath, 2);
	}
	
}



/// <summary>
/// Represents a context associated with the current thread.
/// </summary>
/// <remarks>
/// This class provides a mechanism to manage and track the state of a context tied to the thread that created it.
/// Once the context is ended using the <see cref="End"/> method, it is considered inactive.
/// </remarks>
public sealed class Context {
	internal readonly int ThreadId;
	internal bool Active = true;

	public Context() {
		ThreadId = System.Environment.CurrentManagedThreadId;
	}

	public enum Mode {
		Message,
		Warning,
		Error,
		FileOnly
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
	public void Message(string message, Mode printAs, int frameDepth = 1, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0) {
		if (!Active) throw new InvalidOperationException("Message context is not active.");

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

			string? ns = type.Namespace;
			if (string.IsNullOrEmpty(ns)) return true;

			// Exclude System, Microsoft and Godot namespaces.
			return !ns.StartsWith("System") && !ns.StartsWith("Microsoft") && !ns.StartsWith("Godot");
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
					_ => string.Empty
				};

				string loggedMessage = $"{prefix} {insert}{message}";

				GD.Print(loggedMessage);

				if (printAs == Mode.Warning) GD.PushWarning(loggedMessage);
				else if (printAs == Mode.Error) GD.PushError(loggedMessage);

			}

			// Print only the prefix for intermediate frames.
			else GD.Print(prefix);

			depth++;
			continue;
		}
	}


	/// <summary>
	/// Logs a trace message with the specified context and message content.
	/// </summary>
	/// <param name="message">The message to log. If <see langword="null"/>, an empty string is used.</param>
	public void Log(string? message) {
		Message(message ?? string.Empty, Mode.Message, 2);
	}


	/// <summary>
	/// Logs a trace message with the specified context and message factory.
	/// </summary>
	/// <remarks>
	/// This method is intended for scenarios where the trace message construction is expensive,
	/// as the <paramref name="messageFactory"/> delegate is only evaluated if tracing is enabled.
	/// </remarks>
	/// <param name="messageFactory">
	/// A delegate that generates the trace message.
	/// The delegate is invoked only if tracing is enabled.
	/// </param>
	public void Log(Func<string> messageFactory) {
		Message(messageFactory(), Mode.Message, 2);
	}


	/// <summary>
	/// Logs a warning message to the trace output with the specified context.
	/// </summary>
	/// <param name="message">
	/// The warning message to log.
	/// If <paramref name="message"/> is <see langword="null"/>, an empty string is logged.
	/// </param>
	/// <param name="ctx">
	/// The context in which the trace message is logged.
	/// This provides additional information about the source or scope of the trace.
	/// </param>
	public void Warn(string? message) {
		Message(message ?? string.Empty, Mode.Warning, 2);
	}


	/// <summary>
	/// Logs a warning message to the trace output.
	/// </summary>
	/// <remarks>
	/// This method is intended for scenarios where the trace message construction is expensive,
	/// as the <paramref name="messageFactory"/> delegate is only evaluated if tracing is enabled.
	/// </remarks>
	/// <param name="messageFactory">
	/// A delegate that generates the warning message to be logged.
	/// The delegate is invoked only if tracing is enabled.
	/// </param>
	/// <param name="ctx">
	/// The context in which the trace message is logged.
	/// This provides additional information about the source or scope of the trace.
	/// </param>
	public void Warn(Func<string> messageFactory) {
		Message(messageFactory(), Mode.Warning, 2);
	}


	/// <summary>
	/// Logs an error message with the specified context.
	/// </summary>
	/// <param name="message">
	/// The error message to log.
	/// If <see langword="null"/>, an empty string is logged.
	/// </param>
	/// <param name="ctx">
	/// The context in which the trace message is logged.
	/// This provides additional information about the source or scope of the trace.
	/// </param>
	public void Error(string? message) {
		Message(message ?? string.Empty, Mode.Error, 2);
	}


	/// <summary>
	/// Logs an error message to the trace output using the specified message factory and context.
	/// </summary>
	/// <remarks>
	/// This method is intended for scenarios where the trace message construction is expensive,
	/// as the <paramref name="messageFactory"/> delegate is only evaluated if tracing is enabled.
	/// </remarks>
	/// <param name="ctx">
	/// The context in which the trace message is logged.
	/// This provides additional information about the source or scope of the trace.
	/// </param>
	public void Error(Func<string> messageFactory) {
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

}
