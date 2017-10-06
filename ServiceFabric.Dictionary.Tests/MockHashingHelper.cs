using ServiceFabric.Dictionary.IndexService;

namespace ServiceFabric.Dictionary.Tests
{
    public partial class IndexServiceTests
    {
        /// <summary>
        /// A Mock implementation of <see cref="IHashingHelper"/> that can be used to return predictable output. 
        /// Set <see cref="ReturnValue"/> to control desired hash result.
        /// </summary>
        public class MockHashingHelper : IHashingHelper
        {
            public long ReturnValue { get; set; }

            /// <inheritdoc />
            public long HashString(string input)
            {
                return ReturnValue;
            }
        }
    }
}
