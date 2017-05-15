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

        public static string TagNotFoundOrNotPublished(int id) =>
            $"ID '{id}' does not refer to a published tag";

        public static string ResourceInUse =>
            $"The resource cannot be deleted because it is referenced by other resources";
    }
}
