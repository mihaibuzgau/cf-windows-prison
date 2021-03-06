﻿namespace CloudFoundry.WindowsPrison
{
    using CloudFoundry.WindowsPrison.Native;
    using Microsoft.Win32.SafeHandles;

    public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeTokenHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(this.handle);
        }
    }
}
