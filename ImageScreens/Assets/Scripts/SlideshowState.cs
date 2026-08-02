namespace ImageScreens.Prefabs
{
    // State of a running slideshow.
    //
    // The slideshow is ticked from Blueprint code.
    // This structure holds the state, including the list of images (downloaded from an index file)
    // and the progress in the slideshow.
    public class SlideshowState
    {
        // List of all images that the slideshow is showing,
        // in the original, unshuffled order.
        //
        // The property exists to be able to change the Shuffle configuration property
        // from Shuffled to Unshuffled while a slideshow is running.
        public readonly string[] ImagesOriginal;

        // List of all images that the slideshow is showing,
        // in the order we're showing them.
        //
        // Always a different object than ImagesOriginal.
        public string[] Images;

        // Last returned image.
        public string LastImage;

        // Index of the next image in the Images array that will be shown on the next tick.
        public int NextIndex = 0;

        // Should the order if images be shuffled?
        public bool Shuffle = false;

        // Initialize the structure for a new slideshow with the specified images.
        public SlideshowState(string[] inImages, bool inShuffle)
        {
            ImagesOriginal = inImages;
            Images = (string[]) inImages.Clone();
            NextIndex = 0;
            Shuffle = inShuffle;
        }

        // Reset the structure to clean all previous data.
        public void ResetState()
        {
            Images = new string[0];
            NextIndex = 0;
        }

        // Return the name of the image to show in the next tick.
        //
        // The state will be modified, and another call to NextImage will return a new image.
        public string NextImage()
        {
            // If the next image is zero, we're starting a new run (or the very first
            // run) through the whole array. Shuffle the image list to get a random ordering.
            if (Shuffle && (NextIndex == 0))
            {
                ShuffleImages();
            }

            // Remember the name of the image to use now.
            LastImage = Images[NextIndex];

            // Advance the index.
            NextIndex++;
            if (NextIndex >= Images.Length)
            {
                NextIndex = 0;
            }

            // Return the image.
            return LastImage;
        }

        // Shuffle the Images array.
        private void ShuffleImages()
        {
            for (int i = Images.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (Images[i], Images[j]) = (Images[j], Images[i]);
            }
        }
    }
}
