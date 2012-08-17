// Guids.cs
// MUST match guids.h
using System;

namespace MattC.DockableCLArgs
{
    static class GuidList
    {
        public const string guidDockableCLArgsPkgString = "e24ee617-8b88-44a8-97d9-ca79c690fd03";
        public const string guidDockableCLArgsCmdSetString = "307607f4-f234-4d7d-b5a8-6a18e6b2fbd2";
        public const string guidToolWindowPersistanceString = "1fabd1b6-9281-49cd-82c2-fa8a46ce7f06";

        public static readonly Guid guidDockableCLArgsCmdSet = new Guid(guidDockableCLArgsCmdSetString);
    };
}