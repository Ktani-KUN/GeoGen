﻿using System;

namespace GeoGen.Utilities
{
    /// <summary>
    /// General C# static utilities
    /// </summary>
    public static class GeneralUtilities
    {
        /// <summary>
        /// Swaps the values of two elements, given by their references.
        /// </summary>
        /// <param name="o1">The reference to the first element.</param>
        /// <param name="o2">The reference to the second element.</param>
        public static void Swap<T>(ref T o1, ref T o2)
        {
            var tmp = o1;
            o1 = o2;
            o2 = tmp;
        }

        /// <summary>
        /// Executes a given action a given number of times.
        /// </summary>
        /// <param name="numberOfTimes">The number of times to execute the action.</param>
        /// <param name="action">The action to be executed.</param>
        public static void ExecuteNTimes(int numberOfTimes, Action action)
        {
            // For the given number of times
            for (var i = 0; i < numberOfTimes; i++)
            {
                // Perform the action
                action();
            }
        }
    }
}