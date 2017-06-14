using System;
using System.Collections.Generic;
using System.Linq;
using PaderbornUniversity.SILab.Hip.DataStore.Controllers;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel.Commands
{
    /// <summary>
    /// Provides methods to generate the events that are necessary to create, update and delete exhibit pages.
    /// </summary>
    public static class ExhibitPageCommands
    {
        public static void ValidateExhibitPageArgs(
            ExhibitPageArgs args, AddValidationErrorDelegate addValidationError,
            EntityIndex entityIndex, MediaIndex mediaIndex)
        {
            if (args == null)
                return;

            // constrain properties Image, Images and HideYearNumbers to their respective page types
            if (args.Image != null && args.Type != PageType.AppetizerPage && args.Type != PageType.ImagePage)
                addValidationError(nameof(args.Image),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Image), args.Type));

            if (args.Images != null && args.Type != PageType.SliderPage)
                addValidationError(nameof(args.Images),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Images), args.Type));

            if (args.HideYearNumbers != null && args.Type != PageType.SliderPage)
                addValidationError(nameof(args.HideYearNumbers),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.HideYearNumbers), args.Type));

            // ensure referenced image exists and is published
            if (args.Image != null && !mediaIndex.IsPublishedImage(args.Image.Value))
                addValidationError(nameof(args.Image),
                    ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

            // ensure referenced images exist and are published
            if (args.Images != null)
            {
                var invalidIds = args.Images
                    .Where(id => !mediaIndex.IsPublishedImage(id))
                    .ToList();

                foreach (var id in invalidIds)
                    addValidationError(nameof(args.Images),
                        ErrorMessages.ImageNotFoundOrNotPublished(id));
            }

            // ensure referenced additional info pages exist and are published
            if (args.AdditionalInformationPages != null)
            {
                var invalidIds = args.AdditionalInformationPages
                    .Where(id => entityIndex.Status(ResourceType.ExhibitPage, id) != ContentStatus.Published)
                    .ToList();

                foreach (var id in invalidIds)
                    addValidationError(nameof(args.AdditionalInformationPages),
                        ErrorMessages.ExhibitPageNotFoundOrNotPublished(id));
            }
        }

        private static IEnumerable<IEvent> AddExhibitPageReferences(int pageId, int exhibitId, ExhibitPageArgs args)
        {
            // pages have a reference to the exhibit they belong to, so an exhibit can determine its pages via Exhibit.Referencees
            yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Exhibit, exhibitId);

            if (args.Audio != null)
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, args.Audio.Value);

            if (args.Image != null)
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, args.Image.Value);

            foreach (var imageId in args.Images ?? Enumerable.Empty<int>())
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, imageId);

            foreach (var id in args.AdditionalInformationPages ?? Enumerable.Empty<int>())
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.ExhibitPage, id);
        }

        private static IEnumerable<IEvent> RemoveExhibitPageReferences(int pageId, ReferencesIndex referencesIndex)
        {
            return referencesIndex
                .ReferencesOf(ResourceType.ExhibitPage, pageId)
                .Select(reference => new ReferenceRemoved(ResourceType.ExhibitPage, pageId, reference.Type, reference.Id));
        }

        public static IEnumerable<IEvent> Create(int pageId, int exhibitId, ExhibitPageArgs args)
        {
            var ev = new ExhibitPageCreated
            {
                Id = pageId,
                ExhibitId = exhibitId,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            var addRefEvents = AddExhibitPageReferences(pageId, exhibitId, args);

            // create the page, then add references
            return addRefEvents.Prepend(ev);
        }

        public static IEnumerable<IEvent> Delete(int pageId, ReferencesIndex referencesIndex)
        {
            var ev = new ExhibitPageDeleted { Id = pageId };
            var removeRefEvents = RemoveExhibitPageReferences(pageId, referencesIndex);

            // remove references, then delete the page
            return removeRefEvents.Append(ev);
        }

        public static IEnumerable<IEvent> Update(int pageId, ExhibitPageArgs args, ReferencesIndex referencesIndex, ExhibitPageIndex pageIndex)
        {
            // ReSharper disable once PossibleInvalidOperationException
            var exhibitId = pageIndex.ExhibitId(pageId).Value;

            var removeRefEvents = RemoveExhibitPageReferences(pageId, referencesIndex);

            var ev = new ExhibitPageUpdated
            {
                Id = pageId,
                ExhibitId = exhibitId,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            var addRefEvents = AddExhibitPageReferences(pageId, exhibitId, args);

            // remove old references, then update the page, then add new references
            return removeRefEvents.Append(ev).Concat(addRefEvents);
        }
    }
}
