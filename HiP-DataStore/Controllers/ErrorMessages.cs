using PaderbornUniversity.SILab.Hip.DataStore.Model;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    public static class ErrorMessages
    {
        public static string ImageNotFoundOrNotPublished(int id) =>
            $"ID '{id}' does not refer to a published image";

        public static string AudioNotFoundOrNotPublished(int id) =>
            $"ID '{id}' does not refer to published audio";

        public static string ExhibitNotFoundOrNotPublished(int id) =>
            $"ID '{id}' does not refer to a published exhibit";

        public static string ExhibitPageNotFoundOrNotPublished(int id) =>
            $"ID '{id}' does not refer to a published exhibit page";

        public static string TagNotFoundOrNotPublished(int id) =>
            $"ID '{id}' does not refer to a published tag";

        public static string FieldNotAllowedForPageType(string fieldName, PageType pageType) =>
            $"Field '{fieldName}' not allowed for page type '{pageType}'";

        public static string ResourceInUse =>
            "The resource cannot be deleted because it is referenced by other resources";

        public static string TagNameAlreadyUsed =>
            "A tag with the same name already exists";

        public static string CannotChangeExhibitPageType(PageType currentType, PageType newType) =>
            $"The type of an exhibit page cannot be changed (current type is '{currentType}', attempted to change to '{newType}')";

        public static string ExhibitPageOnlyReorderAllowed =>
            "The set of specified page IDs differs from the set of pages actually belonging to the exhibit. Addition and removal of pages is not supported by the PUT API, only reordering of pages is.";
    }
}
