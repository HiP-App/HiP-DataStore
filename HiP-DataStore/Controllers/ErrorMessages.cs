using PaderbornUniversity.SILab.Hip.DataStore.Model;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    public static class ErrorMessages
    {
        public static string ImageNotFound(int id) =>
            $"No image with ID '{id}' exists";

        public static string AudioNotFound(int id) =>
            $"No audio with ID '{id}' exists";

        public static string ExhibitNotFound(int id) =>
            $"No exhibit with ID '{id}' exists";

        public static string ExhibitPageNotFound(int id) =>
            $"No page with ID '{id}' exists";

        public static string TagNotFound(int id) =>
            $"No tag with ID '{id}' exists";

        public static string FieldNotAllowedForPageType(string fieldName, PageType pageType) =>
            $"Field '{fieldName}' not allowed for page type '{pageType}'";

        public static string ResourceInUse =>
            "The resource cannot be deleted because it is referenced by other resources";

        public static string TagNameAlreadyUsed =>
            "A tag with the same name already exists";

        public static string CannotChangeExhibitPageType(PageType currentType, PageType newType) =>
            $"The type of an exhibit page cannot be changed (current type is '{currentType}', attempted to change to '{newType}')";
    }
}
