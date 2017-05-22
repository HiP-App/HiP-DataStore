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
    }
}
