namespace PM.Plugins
{
   /// <summary>
   /// Represents a single preference item in the PmPrefs editor.
   /// Tracks the key, value, and modification state of a preference.
   /// </summary>
   [System.Serializable]
   public class PmPrefsListItem
   {
      /// <summary>
      /// When true, this item is marked for deletion on save.
      /// </summary>
      public bool DeleteMarker;

      /// <summary>
      /// The preference key (without prefix for PmPrefs items).
      /// </summary>
      public string Key;

      /// <summary>
      /// The current value of the preference.
      /// </summary>
      public string Value;

      private string _initial;

      /// <summary>
      /// Returns true if the value has been modified since last save.
      /// </summary>
      public bool Changed => Value != _initial;

      /// <summary>
      /// Marks the current value as saved (resets change tracking).
      /// </summary>
      public void Save() => _initial = Value;

      /// <summary>
      /// Resets the value to its last saved state.
      /// </summary>
      public void Reset() => Value = _initial;

      /// <summary>
      /// Creates a new preference list item.
      /// </summary>
      /// <param name="key">The preference key.</param>
      /// <param name="value">The preference value.</param>
      public PmPrefsListItem(string key, string value)
      {
         Key = key;
         _initial = Value = value;
      }
   }
}
