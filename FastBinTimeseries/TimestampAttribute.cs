using System;
using NYurik.FastBinTimeseries.CommonCode;

namespace NYurik.FastBinTimeseries
{
    /// <summary>
    /// Use this attribute to specify which of the <see cref="UtcDateTime"/> fields to use as a timestamp
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class TimestampAttribute : Attribute
    {
    }
}