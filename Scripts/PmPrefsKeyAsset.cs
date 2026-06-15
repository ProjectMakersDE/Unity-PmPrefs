using UnityEngine;

namespace PM.Plugins
{
   /// <summary>
   /// Configuration asset for PmPrefs. Stores the active encryption key so it can be
   /// changed at edit time without rewriting source code, and read back at runtime.
   /// </summary>
   /// <remarks>
   /// The asset is resolved from any <c>Resources</c> folder at runtime via
   /// <see cref="Resources.LoadAll(string)"/>. Leave <see cref="secureKey"/> empty to fall
   /// back to the built-in default key. Use the PmPrefs editor window (Configuration &gt;
   /// Secure Key) to create or update this asset; it is written into the project's
   /// <c>Assets/</c> folder so it works even when the package itself is read-only.
   /// </remarks>
   [CreateAssetMenu(fileName = "PmPrefsKeyAsset", menuName = "ProjectMakers/PmPrefs Config")]
   public class PmPrefsKeyAsset : ScriptableObject
   {
      /// <summary>
      /// The active encryption key. Empty means "use the built-in default key".
      /// Must be at least 8 alphanumeric characters when set.
      /// </summary>
      [Tooltip("Encryption key for PmPrefs. Leave empty to use the built-in default key. Min 8 alphanumeric characters.")]
      public string secureKey;
   }
}
