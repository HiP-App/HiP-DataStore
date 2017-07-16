using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    /// <summary>
    /// Makes MVC services available to the test classes.
    /// </summary>
    /// <remarks>
    /// In <see cref="TestStartup"/> we register services for MVC. These somehow need to be made available
    /// to the tests (e.g. <see cref="ControllerTests.TagsControllerTest"/>) so the tests can validate the
    /// REST API results. Since there doesn't seem to be a recommended way, here's a quick and dirty solution.
    /// </remarks>
    public static class MvcTestContext
    {
        private static IServiceProvider _services;

        public static IServiceProvider Services
        {
            get
            {
                if (_services == null)
                    throw new InvalidOperationException("MVC services are not (yet) available. Try calling the REST API first (via MyMvc) and then access this property.");

                return _services;
            }
            set => _services = value;
        }

        public static T GetService<T>(this IServiceProvider services) =>
            (T)services.GetService(typeof(T));
    }
}
