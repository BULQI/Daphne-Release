using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaphneGui
{
    /// <summary>
    /// all dividable entities should implement this interface
    /// </summary>
    public interface IDividable<T>
    {
        // allow for optional user data
        T Divide(double data = 0);
    }

    /// <summary>
    /// dynamic entities should implement this interface
    /// </summary>
    public interface IDynamic
    {
        void Step(double dt);
    }
}
