using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;

namespace CommonScripts;

public partial class Log : Node {
    #region Static logging

    /// <summary>
    /// Logs a message to the console with additional context information.
    /// </summary>
    /// <param name="message">
    /// The message to log.
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
    public static string Message(
        string message,
        bool enabled = true,
        int stack = 0,
        bool useFilePath = false,
        int frame = 1,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0
    ) {
        string className, methodName, prefix;
        string padding = new(' ', stack);

        MethodBase? method = new StackTrace(frame, false).GetFrame(0)?.GetMethod();

        //Attempt getting the class and method name from the stack trace.
        if (method != null && !useFilePath) {
            className = method.DeclaringType?.Name ?? string.Empty;
            methodName = method.Name;
            prefix = $"{padding}[{className}.{methodName}]";
        }

        //Use the file path, method name, and line number.
        else {
            string fileName = Path.GetFileName(filePath);
            prefix = $"{padding}[{fileName} @ line {line}]";
        }

        string loggedMessage = $"{prefix} {message}";
        if (enabled) GD.Print(loggedMessage); //TODO: Replace with a proper logging framework.

        return loggedMessage;
    }

    /// <summary>
    /// Logs a message to the console with additional context information.
    /// </summary>
    /// <param name="message">
    /// The message to log.
    /// </param>
    /// <param name="useFilePath">
    /// Whether to use the file path and line number for logging instead of the class and method names.
    /// </param>
    public static void Me(string message, bool enabled = true, int stack = 0, bool useFilePath = false) {
        if (!enabled) return;
        Message(message, enabled, stack, useFilePath, 2);
    }

    /// <summary>
    /// Logs a message to the console with additional context information. <br/>
    /// This is best used for logging messages that don't always need to be logged,
    /// saving computation time when the message <c>enabled = false</c>.
    /// </summary>
    /// <param name="messageFactory"></param>
    /// <param name="enabled"></param>
    /// <param name="stack"></param>
    /// <param name="useFilePath"></param>
    public static void Me(Func<string> messageFactory, bool enabled = true, int stack = 0, bool useFilePath = false) {
        if (!enabled) return;
        Message(messageFactory(), enabled, stack, useFilePath, 2);
    }

    #endregion


}

