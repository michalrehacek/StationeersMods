using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace ImageScreens.Prefabs
{
    // Base class for all image screens.
    //
    // It holds all logic, except logic that has to differ between all variants.
    public abstract class ImageScreenBase : SmallDevice, DownloadCache.IListener
    {
        // Has the object been destroyed?
        //
        // Used as a guard to detect texture loaded callbacks into a Unity object that is destroyed.
        // Screens are removed from download callbacks when destroyed,
        // so this flag is only a last-resort defense against crashes in unexpected situations.
        private bool destroyed = false;
        // Renderer of the screen image.
        protected MeshRenderer screenRenderer;

        // Specicifaction about what the screen should display.
        protected ImageScreenSpec spec;

        // Error originating on the client.
        protected string ClientError;
        // Error originating on the server.
        protected string ServerError;

        // Currently running slideshow, or null if none.
        protected SlideshowState SlideState = null;
        // Coroutine running the slideshow, if any.
        protected Coroutine SlideTimer = null;
        // Delay between slides in a slideshow, in seconds.
        protected int SlideDelay = 5;

        // Callback when the object is created and ready to live.
        public override void Awake()
        {
            base.Awake();

            var screen = transform.Find("Screen");
            screenRenderer = screen.GetComponent<MeshRenderer>();

            CreateSpecFromName();
        }

        // Callback when the object is destroyed.
        public override void OnDestroy()
        {
            base.OnDestroy();
            ImageScreens.Instance.Cache.ListenerDestroyed(this);
            destroyed = true;
        }

        // Download a new image, when the screen is renamed.
        public override void OnRenamed()
        {
            base.OnRenamed();
            CreateSpecFromName();
        }

        // Significantly increase the rendering distance.
        protected override float GetRenderMaxDistanceSquared()
        {
            return 10000f; // same as Frame
        }

        // Significantly increase the shadow distance.
        protected override float GetShadowMaxDistanceSquared()
        {
            return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f); // same as Frame
        }

        // Create a new spec after the external properties (like custom name) changed,
        // and update whatever is needed on the screen to display the new spec.
        protected void CreateSpecFromName()
        {
            ClearError();

            spec = new ImageScreenSpec()
            {
                URL = CustomName, // URL comes from the CustomName, set using the Labeller.
            };
            UpdateImage();
        }

        // Update the image we are displaying, based on the spec.
        //
        // The function will initiate a download if needed.
        private void UpdateImage()
        {
            // End the current slideshow, if one is running.
            EndSlideShow();

            // If the name is empty (which is the default), use the default empty image.
            if (string.IsNullOrEmpty(spec.URL))
            {
                DisplayTexture(ImageScreens.Instance.EmptyTexture);
                return;
            }

            // If the URL is not valid, display an error.
            if ( ! Utils.IsValidURL(spec.URL) )
            {
                SetClientError($"Invalid URL \"{spec.URL}\".");
                DisplayTexture(ImageScreens.Instance.ErrorTexture);
                return;
            }

            // Load the new URL (slideshow or image).
            ImageScreens.Instance.Cache.DownloadFile(this, spec.URL);
        }

        // Callback when the texture download finishes (success or fail).
        public void DownloadFinished( DownloadCache.Result result )
        {
            // Callback to a screen that has already been destroyed.
            if ( destroyed )
            {
                Log.Info($"Download callback for \"{result.URL}\": ignoring, the screen has been destroyed.");
                return;
            }

            // Has the screen specification change, since we initiated the download?
            // If yes, don't update the screen - a new task has been initiated in the later rename that will do it.
            if ( ! IsExpectingDownload(result.URL) )
            {
                Log.Info($"Download callback for \"{result.URL}\": ignoring, the screen has been renamed since the download started.");
                return;
            }

            // Did the download fail?
            if ( ! result.Success )
            {
                Log.Info($"Download callback for \"{result.URL}\" failed, setting error texture.");
                SetClientError(string.Format(ModStrings.FailedToDownload,result.URL));
                DisplayTexture(ImageScreens.Instance.ErrorTexture);
                return;
            }

            // Download successful.
            ClearError();
            Log.Info($"Download callback for \"{result.URL}\", success.");
            if (result.Data.IsText)
            {
                StartSlideshow(result.Data);
            }
            else
            {
                DisplayTexture(result.Data.Texture);
            }
        }

        // Are we expecting a download of this URL?
        private bool IsExpectingDownload(string URL)
        {
            // Downloading the URL the user entered is always OK.
            if (URL == spec.URL)
            {
                return true;
            }

            // If a slideshow is active, then the last downloaded image is also OK.
            if ( ( SlideState != null ) && ( URL == SlideState.LastImage ) )
            {
                return true;
            }

            return false;
        }

        // Display the specified texture on the screen.
        private void DisplayTexture(Texture newTexture)
        {
            // Set the texture to the material.
            var material = screenRenderer.materials[ 1 ];
            material.mainTexture = newTexture;

            // Update the emission map to the same texture, if the material uses one.
            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", newTexture);
            }
        }

        // Serialize the screen to a save data object.
        public override ThingSaveData SerializeSave()
        {
            ThingSaveData save = new ImageScreenSaveData();
            InitialiseSaveData(ref save);
            return save;
        }

        // Save the screen to the provided save data.
        protected override void InitialiseSaveData(ref ThingSaveData savedData)
        {
            base.InitialiseSaveData(ref savedData);
            if (savedData is ImageScreenSaveData save)
            {
                // Save the specification of what we're showing.
                save.Spec = spec;
            }
        }

        // Load the screen from the provided save data.
        public override void DeserializeSave(ThingSaveData data)
        {
            base.DeserializeSave(data);
            if (data is ImageScreenSaveData save)
            {
                // Load the specification of what we're showing.
                spec = save.Spec;

                // Initialize the download.
                UpdateImage();
            }
        }

        // Return an extended tooltip text.
        public override StringBuilder GetExtendedText()
        {
            StringBuilder builder = new StringBuilder();
            AppendError(builder, ServerError);
            AppendError(builder, ClientError);
            builder.Append(base.GetExtendedText());
            return builder;
        }

        // Append the specified error to the string builder, if it's not empty.
        private static void AppendError( StringBuilder builder, string error )
        {
            if (!string.IsNullOrEmpty(error))
            {
                string line = string.Format(ModStrings.Error,error);
                builder.AppendLine(line);
            }
        }

        // Do we have any error to show?
        public bool HasError()
        {
            return ! string.IsNullOrEmpty( ClientError ) || ! string.IsNullOrEmpty( ServerError );
        }

        // Clear all error strings.
        private void ClearError()
        {
            ClientError = null;
            ServerError = null;
        }

        // Remember this client-side error.
        private void SetClientError( string error )
        {
            ClientError = error;
        }

        // Start a new slideshow.
        private void StartSlideshow(DownloadCache.Entry data)
        {
            Assert.IsNull(SlideTimer,"ImageScreens mod: Starting a slideshow when one is already running");

            // Create a co-routine to tick the slideshow and initiate the first image download.
            SlideState = new SlideshowState(data.Index,false);
            SlideTimer = StartCoroutine(SlideshowWorker());
        }

        // Co-routine to tick the slideshow.
        private IEnumerator SlideshowWorker()
        {
            for (; ;)
            {
                SlideshowTick();
                yield return new WaitForSeconds(SlideDelay);
            }
        }

        // Perform one tick of a slideshow - load one image.
        private void SlideshowTick()
        {
            // Get the next image and verify it's an image.
            string url = SlideState.NextImage();
            if (!Utils.IsImageURL(url))
            {
                SetClientError($"Slideshow image \"{url}\" is not an image.");
                DisplayTexture(ImageScreens.Instance.ErrorTexture);
                return;
            }

            // Initiate the image download.
            ImageScreens.Instance.Cache.DownloadFile(this, url);
        }

        // End the current slideshow, if one is running.
        private void EndSlideShow()
        {
            if (SlideTimer != null)
            {
                Log.Info($"Stopped a running slideshow from \"{spec.URL}");
                StopCoroutine(SlideTimer);
                SlideState = null;
                SlideTimer = null;
            }
        }
    }
}
