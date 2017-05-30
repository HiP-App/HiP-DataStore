namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel.Commands
{
    /// <summary>
    /// The signature of a method that adds a validation error message to a collection of error messages, such as a
    /// <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>.
    /// </summary>
    /// <param name="key">The name of the property of which the validation failed</param>
    /// <param name="errorMessage">A description of the error</param>
    public delegate void AddValidationErrorDelegate(string key, string errorMessage);
}
