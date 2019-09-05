﻿using GeoGen.Constructor;
using GeoGen.Core;
using GeoGen.TheoremsFinder;
using System.Collections.Generic;
using System.IO;

namespace GeoGen.ConsoleLauncher
{
    /// <summary>
    /// A default settings for the application.
    /// </summary>
    public class DefaultSettings : Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSettings"/> class.
        /// </summary>
        public DefaultSettings()
        {
            // The folder in which we're working by default
            var workingFolder = "..\\..\\..\\Data";

            // Create the loggers
            Loggers = new List<BaseLoggerSettings>
            {
                // Console logger
                new ConsoleLoggerSettings
                {
                    IncludeLoggingOrigin = false,
                    IncludeTime = true,
                    LogOutputLevel = LogOutputLevel.Info
                },

                // File logger
                new FileLoggerSettings
                {
                    IncludeLoggingOrigin = true,
                    IncludeTime = true,
                    LogOutputLevel = LogOutputLevel.Debug,
                    FileLogPath = "log.txt"
                }
            };

            // Create default manager settings
            PicturesManagerSettings = new PicturesSettings
            {
                NumberOfPictures = 5,
                MaximalAttemptsToReconstructOnePicture = 0,
                MaximalAttemptsToReconstructAllPictures = 0
            };

            // Create default folder settings
            InputFolderSettings = new InputFolderSettings
            {
                InputFolderPath = workingFolder,
                InputFilePrefix = "input",
                FilesExtention = "txt",
            };

            // Create default algorithm runner settings
            AlgorithmRunnerSettings = new AlgorithmRunnerSettings
            {
                OutputFolder = workingFolder,
                OutputFileExtention = "txt",
                OutputFilePrefix = "output",
                GenerationProgresLoggingFrequency = 100,
                LogProgress = true,
                GenerateFullReport = true,
                FullReportSuffix = " (full)"
            };

            // Create default template theorems folder settings
            TemplateTheoremsFolderSettings = new TemplateTheoremsFolderSettings
            {
                TheoremsFolderPath = Path.Combine(workingFolder, "TemplateTheorems"),
                FilesExtention = "txt"
            };

            // Create settings for tangent circles theorems finder
            TangentCirclesTheoremsFinderSettings = new TangentCirclesTheoremsFinderSettings
            {
                ExcludeTangencyInsidePicture = true
            };

            // Create settings for line tangent to circle theorems finder
            LineTangentToCircleTheoremsFinderSettings = new LineTangentToCircleTheoremsFinderSettings
            {
                ExcludeTangencyInsidePicture = true
            };

            // Set the list of theorem types that we're looking for
            SeekedTheoremTypes = new[]
            {
                TheoremType.CollinearPoints,
                TheoremType.ConcurrentLines,
                TheoremType.ConcyclicPoints,
                TheoremType.EqualLineSegments,
                TheoremType.LineTangentToCircle,
                TheoremType.ParallelLines,
                TheoremType.PerpendicularLines,
                TheoremType.TangentCircles
            };
        }
    }
}