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