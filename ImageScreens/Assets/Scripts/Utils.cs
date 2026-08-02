using System;

namespace ImageScreens
{
    // Utility functions for image screens.
    public static class Utils
    {
        // Is the specified URL valid, i.e. is this even an URL?
        public static bool IsValidURL( string URL )
        {
            return
                URL.StartsWith("http:") ||
                URL.StartsWith("https:");
        }

        // Does the specified URL contain a text file?
        public static bool IsTextURL( string URL )
        {
            return
                IsValidURL(URL) &&
                URL.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase);
        }

        // Does the specified URL contain an image file?
        //
        // A non-empty URL that isn't Text URL is considered to be an image by default.
        public static bool IsImageURL( string URL )
        {
            return
                IsValidURL(URL) &&
                !IsTextURL(URL);
        }

        // Return the prefix of the URL path, i.e. the directory that the index file was downloaded from.
        public static string GetURLPrefix( string URL )
        {
            int lastSlash = URL.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                // Get the string up to and including the last slash.
                return URL.Substring(0, lastSlash + 1);
            }
            else
            {
                // No slash found in the URL, which is a really weird URL.
                // Just return the original URL with a slash, to make it look like a directory...
                return URL + "/";
            }
        }
    }
}
