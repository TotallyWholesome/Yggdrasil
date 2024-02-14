﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Yggdrasil.Logging;

namespace Yggdrasil.Util
{
	/// <summary>
	/// Console utility functions.
	/// </summary>
	public class ConsoleUtil
	{
		/// <summary>
		/// Writes logo and credits to Console.
		/// </summary>
		/// <param name="consoleTitlePrefix">Software name.</param>
		/// <param name="consoleTitle">Name of the server.</param>
		/// <param name="logoColor">Color of the logo.</param>
		/// <param name="logo">ASCII logo.</param>
		/// <param name="credits">Credits for header footer.</param>
		public static void WriteHeader(string consoleTitlePrefix, string consoleTitle, ConsoleColor logoColor, IList<string> logo, IList<string> credits)
		{
			var foreColor = Console.ForegroundColor;

			Console.Title = consoleTitlePrefix + " : " + consoleTitle;

			Console.ForegroundColor = logoColor;
			WriteLinesCentered(logo);

			Console.WriteLine();

			Console.ForegroundColor = ConsoleColor.White;
			WriteLinesCentered(credits);

			Console.ForegroundColor = foreColor;

			WriteSeperator();
		}

		/// <summary>
		/// Writes seperator in form of horizontal line to Console.
		/// </summary>
		public static void WriteSeperator()
		{
			Console.WriteLine("".PadLeft(Console.WindowWidth, '_'));
		}

		/// <summary>
		/// Writes lines to Console, centering them as a group.
		/// </summary>
		/// <param name="lines"></param>
		private static void WriteLinesCentered(IList<string> lines)
		{
			var longestLine = lines.Max(a => a.Length);
			foreach (var line in lines)
				WriteLineCentered(line, longestLine);
		}

		/// <summary>
		/// Writes line to Console, centering it either with the string's
		/// length or the given length as reference.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="referenceLength">Set to greater than 0, to use it as reference length, to align a text group.</param>
		private static void WriteLineCentered(string line, int referenceLength = -1)
		{
			if (referenceLength < 0)
				referenceLength = line.Length;

			Console.WriteLine(line.PadLeft(line.Length + Console.WindowWidth / 2 - referenceLength / 2));
		}

		/// <summary>
		/// Prefixes window title with an asterisk, to indicate that the
		/// application is not yet ready to use.
		/// </summary>
		public static void LoadingTitle()
		{
			if (!Console.Title.StartsWith("* "))
				Console.Title = "* " + Console.Title;
		}

		/// <summary>
		/// Removes asterisks and spaces that were prepended to the window
		/// title, to indicate that the application is fully loaded and ready
		/// now.
		/// </summary>
		public static void RunningTitle()
		{
			Console.Title = Console.Title.TrimStart('*', ' ');
		}

		/// <summary>
		/// Closes the application with the given exit code. If wait is true,
		/// and the application is running in a visible console, it waits for
		/// Return to be pressed before exiting.
		/// </summary>
		/// <param name="exitCode"></param>
		/// <param name="wait"></param>
		public static void Exit(int exitCode, bool wait = true)
		{
			if (wait && IsUserInteractive)
			{
				Log.Info("Press Enter to exit.");
				Console.ReadLine();
			}
			Log.Info("Exiting...");
			Environment.Exit(exitCode);
		}

		/// <summary>
		/// Returns whether the application is running on Mono.
		/// </summary>
		public static bool CheckMono()
		{
			return (Type.GetType("Mono.Runtime") != null);
		}

		/// <summary>
		/// Gets a value indicating whether the current process is running
		/// in user interactive mode.
		/// </summary>
		/// <remarks>
		/// Custom property wrapping Environment.UserInteractive, with special
		/// behavior for Mono, which currently doesn't support that property.
		/// </remarks>
		/// <returns></returns>
		public static bool IsUserInteractive
		{
			get
			{
#if __MonoCS__
				// "In" is CStreamReader when running normally
				// (TextReader on Windows) and SynchronizedReader
				// when running in background.
				return (Console.In is System.IO.StreamReader);
#else
				return Environment.UserInteractive;
#endif
			}
		}
	}
}
