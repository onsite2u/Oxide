﻿using System.Reflection;

using Oxide.Core.Libraries;
using Oxide.Plugins;

namespace Oxide.Game.Unturned.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Unturned
    /// </summary>
    public class Unturned : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        /// <returns></returns>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets private bindingflag for accessing private methods, fields, and properties
        /// </summary>
        [LibraryFunction("PrivateBindingFlag")]
        public BindingFlags PrivateBindingFlag() => (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        [LibraryFunction("QuoteSafe")]
        public string QuoteSafe(string str) => str.Quote();
    }
}
