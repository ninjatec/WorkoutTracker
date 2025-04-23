/// <summary>
/// Document.cs - XML Documentation Example
/// This file serves as a reference for documenting public APIs in the project.
/// </summary>
namespace WorkoutTrackerWeb.Documentation
{
    /// <summary>
    /// Interface template showing proper XML documentation format for interfaces.
    /// </summary>
    /// <remarks>
    /// Use the remarks section for more detailed information about the interface.
    /// Include usage examples or important implementation notes here.
    /// </remarks>
    public interface IExampleInterface
    {
        /// <summary>
        /// Gets or sets a sample property with full documentation.
        /// </summary>
        /// <value>The value description goes here.</value>
        string SampleProperty { get; set; }

        /// <summary>
        /// Sample method showing parameter and return value documentation.
        /// </summary>
        /// <param name="input">Description of the input parameter.</param>
        /// <returns>Description of the return value.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when input is null.</exception>
        /// <example>
        /// <code>
        /// var result = instance.SampleMethod("test");
        /// Console.WriteLine(result);
        /// </code>
        /// </example>
        string SampleMethod(string input);
    }

    /// <summary>
    /// Class template showing proper XML documentation format for classes.
    /// </summary>
    /// <remarks>
    /// The remarks section can include more detailed information and examples
    /// on how to use this class effectively.
    /// </remarks>
    public class ExampleClass
    {
        /// <summary>
        /// Private field documentation example.
        /// </summary>
        private readonly string _privateField;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClass"/> class.
        /// </summary>
        /// <param name="value">The initial value for the private field.</param>
        public ExampleClass(string value)
        {
            _privateField = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a public property with documentation.
        /// </summary>
        /// <value>The value description.</value>
        public string PublicProperty { get; set; } = string.Empty;

        /// <summary>
        /// Performs a sample operation and returns a result.
        /// </summary>
        /// <param name="input">The input parameter.</param>
        /// <returns>A string result based on the input.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when input is null.</exception>
        /// <remarks>
        /// This method combines the input with the private field value.
        /// </remarks>
        public string DoSomething(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return $"{_privateField}: {input}";
        }

        /// <summary>
        /// Asynchronous method documentation example.
        /// </summary>
        /// <param name="delay">The delay in milliseconds.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DoSomethingAsync(int delay)
        {
            await Task.Delay(delay);
        }

        /// <summary>
        /// Generic method documentation example.
        /// </summary>
        /// <typeparam name="T">The type parameter, preferably a class with a parameterless constructor.</typeparam>
        /// <param name="input">The input value to process.</param>
        /// <returns>A new instance of T with properties set from input.</returns>
        public T ProcessData<T>(string input) where T : class, new()
        {
            // Implementation details
            return new T();
        }
    }

    /// <summary>
    /// Enumeration documentation example.
    /// </summary>
    public enum ExampleEnum
    {
        /// <summary>
        /// First option in the enumeration.
        /// </summary>
        Option1 = 0,

        /// <summary>
        /// Second option in the enumeration.
        /// </summary>
        Option2 = 1,

        /// <summary>
        /// Third option with a specific value.
        /// </summary>
        Option3 = 10
    }
}