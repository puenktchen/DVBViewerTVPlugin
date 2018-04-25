﻿using System;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public class ExceptionHelper
    {
        /// <summary>
        /// Creates an argument exception.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static ArgumentException CreateArgumentException(String argumentName, String message, params Object[] args)
        {
            return new ArgumentException(
                String.Format("There was a problem with the argument {0} - {1}", 
                    argumentName, 
                    String.Format(message, args)));
        }
    }
}
