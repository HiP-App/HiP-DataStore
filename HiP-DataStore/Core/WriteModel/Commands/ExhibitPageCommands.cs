using System;
using System.Collections.Generic;
using System.Linq;
using PaderbornUniversity.SILab.Hip.DataStore.Controllers;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel.Commands
{
    /// <summary>
    /// Provides methods to generate the events that are necessary to create, update and delete exhibit pages.
    /// </summary>
    public static class ExhibitPageCommands
    {
        public static void ValidateExhibitPageArgs(
            ExhibitPageArgs2 args, AddValidationErrorDelegate addValidationError,
            EntityIndex entityIndex, MediaIndex mediaIndex,
            ExhibitPagesConfig exhibitPagesConfig)
        {
            if (args == null)
                return;

            // constrain properties Image, Images and HideYearNumbers to their respective page types
            if (args.Image != null && args.Type != PageType.Appetizer_Page && args.Type != PageType.Image_Page)
                addValidationError(nameof(args.Image),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Image), args.Type));

            if (args.Images != null && args.Type != PageType.Slider_Page)
                addValidationError(nameof(args.Images),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Images), args.Type));

            if (args.HideYearNumbers != null && args.Type != PageType.Slider_Page)
                addValidationError(nameof(args.HideYearNumbers),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.HideYearNumbers), args.Type));

            // validate font family
            if (!exhibitPagesConfig.IsFontFamilyValid(args.FontFamily))
                addValidationError(nameof(args.FontFamily), $"Font family must be null/unspecified (which defaults to {exhibitPagesConfig.DefaultFontFamily}) or one of the following: {string.Join(", ", exhibitPagesConfig.FontFamilies)}");

            // ensure referenced image exists
            if (args.Image != null && !mediaIndex.IsImage(args.Image.Value))
                addValidationError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));

            // ensure referenced slider page images exist
            if (args.Images != null)
            {
                var invalidIds = args.Images
                    .Select(img => img.Image)
                    .Where(id => !mediaIndex.IsImage(id))
                    .ToList();

                foreach (var id in invalidIds)
                    addValidationError(nameof(args.Images),
                        ErrorMessages.ImageNotFound(id));
            }

            // ensure referenced additional info pages exist
            if (args.AdditionalInformationPages != null)
            {
                var invalidIds = args.AdditionalInformationPages
                    .Where(id => !entityIndex.Exists(ResourceType.ExhibitPage, id))
                    .ToList();

                foreach (var id in invalidIds)
                    addValidationError(nameof(args.AdditionalInformationPages),
                        ErrorMessages.ExhibitPageNotFound(id));
            }
        }

        private static IEnumerable<IEvent> AddExhibitPageReferences(int pageId, ExhibitPageArgs2 args)
        {
            if (args.Audio != null)
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, args.Audio.Value);

            if (args.Image != null)
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, args.Image.Value);

            foreach (var img in args.Images?.Distinct() ?? Enumerable.Empty<SliderPageImageArgs>())
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, img.Image);

            foreach (var id in args.AdditionalInformationPages?.Distinct() ?? Enumerable.Empty<int>())
                yield return new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.ExhibitPage, id);
        }

        private static IEnumerable<IEvent> RemoveExhibitPageReferences(int pageId, ReferencesIndex referencesIndex)
        {
            return referencesIndex
                .ReferencesOf(ResourceType.ExhibitPage, pageId)
                .Select(reference => new ReferenceRemoved(ResourceType.ExhibitPage, pageId, reference.Type, reference.Id));
        }

        public static IEnumerable<IEvent> Create(int pageId, ExhibitPageArgs2 args)
        {
            var ev = new ExhibitPageCreated3
            {
                Id = pageId,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            var addRefEvents = AddExhibitPageReferences(pageId, args);

            // create the page, then add references
            return addRefEvents.Prepend(ev);
        }

        public static IEnumerable<IEvent> Delete(int pageId, ReferencesIndex referencesIndex)
        {
            var ev = new ExhibitPageDeleted2 { Id = pageId };
            var removeRefEvents = RemoveExhibitPageReferences(pageId, referencesIndex);

            // remove references, then delete the page
            return removeRefEvents.Append(ev);
        }

        public static IEnumerable<IEvent> Update(int pageId, ExhibitPageArgs2 args, ReferencesIndex referencesIndex)
        {
            var removeRefEvents = RemoveExhibitPageReferences(pageId, referencesIndex);

            var ev = new ExhibitPageUpdated3
            {
                Id = pageId,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            var addRefEvents = AddExhibitPageReferences(pageId, args);

            // remove old references, then update the page, then add new references
            return removeRefEvents.Append(ev).Concat(addRefEvents);
        }
    }
}
