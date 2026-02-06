using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PM.Plugins
{
   /// <summary>
   /// UI controller for a single preference item in the list view.
   /// Handles data binding and visual feedback for changes.
   /// </summary>
   public class PmPrefsListItemEntryController
   {
      private Label _keyLabel;
      private TextField _valueField;
      private Toggle _deleteToggle;
      private Button _copyKeyButton;
      private Button _copyValueButton;
      private bool _isChanged;

      private PmPrefsListItem _data;
      private VisualElement _root;

      // Style colors
      private static readonly Color ChangedBorderColor = new Color(0.35f, 0.61f, 0.3f);
      private static readonly Color DeleteBorderColor = new Color(0.6f, 0.27f, 0.27f);
      private static readonly Color TransparentColor = new Color(0, 0, 0, 0);
      private static readonly Color CopyFeedbackColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);

      /// <summary>
      /// Sets up the visual element references for this controller.
      /// </summary>
      /// <param name="visualElement">The root visual element for this list item.</param>
      public void SetVisualElement(VisualElement visualElement)
      {
         _root = visualElement;
         _keyLabel = visualElement.Q<Label>("Item_name");
         _valueField = visualElement.Q<TextField>("Item_value");
         _deleteToggle = visualElement.Q<Toggle>("Item_delete");
         _copyKeyButton = visualElement.Q<Button>("Item_copy_key");
         _copyValueButton = visualElement.Q<Button>("Item_copy_value");
      }

      /// <summary>
      /// Gets the current value from the text field.
      /// </summary>
      public string GetValue() => _valueField.value;

      /// <summary>
      /// Gets the current delete state.
      /// </summary>
      public bool GetDelete() => _deleteToggle.value;

      /// <summary>
      /// Gets the key name.
      /// </summary>
      public string GetKey() => _keyLabel.text;

      /// <summary>
      /// Returns true if the value has been changed in the UI.
      /// </summary>
      public bool GetChanged() => _isChanged;

      /// <summary>
      /// Sets the visibility of this list item.
      /// </summary>
      /// <param name="visible">True to show, false to hide.</param>
      public void SetVisibility(bool visible)
      {
         _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      }

      /// <summary>
      /// Binds data to this list item controller.
      /// </summary>
      /// <param name="data">The preference data to display.</param>
      public void SetData(PmPrefsListItem data)
      {
         _data = data;
         Initialize();
      }

      private void Initialize()
      {
         // Reset state
         _isChanged = false;

         // Unregister old callbacks to prevent duplicates
         _valueField.UnregisterValueChangedCallback(OnValueChanged);
         _deleteToggle.UnregisterValueChangedCallback(OnDeleteChanged);
         if (_copyKeyButton != null)
         {
            _copyKeyButton.clicked -= OnCopyKeyClicked;
         }
         if (_copyValueButton != null)
         {
            _copyValueButton.clicked -= OnCopyValueClicked;
         }

         // Set initial values
         _keyLabel.text = _data.Key;
         _valueField.SetValueWithoutNotify(_data.Value);
         _deleteToggle.SetValueWithoutNotify(_data.DeleteMarker);

         // Reset visual state
         ClearBorderStyle();

         // Apply existing state visuals
         if (_data.DeleteMarker)
         {
            ApplyBorderStyle(DeleteBorderColor);
         }
         else if (_data.Changed)
         {
            ApplyBorderStyle(ChangedBorderColor);
         }

         // Update height based on content
         UpdateHeight();

         // Register callbacks
         _valueField.RegisterValueChangedCallback(OnValueChanged);
         _deleteToggle.RegisterValueChangedCallback(OnDeleteChanged);

         // Wire up copy button handlers and set tooltips
         if (_copyKeyButton != null)
         {
            _copyKeyButton.clicked += OnCopyKeyClicked;
            _copyKeyButton.tooltip = "Copy key to clipboard";
         }
         if (_copyValueButton != null)
         {
            _copyValueButton.clicked += OnCopyValueClicked;
            _copyValueButton.tooltip = "Copy value to clipboard";
         }
      }

      private void OnDeleteChanged(ChangeEvent<bool> evt)
      {
         _data.DeleteMarker = evt.newValue;

         if (_data.DeleteMarker)
         {
            ApplyBorderStyle(DeleteBorderColor);
         }
         else if (_data.Changed)
         {
            ApplyBorderStyle(ChangedBorderColor);
         }
         else
         {
            ClearBorderStyle();
         }
      }

      private void OnValueChanged(ChangeEvent<string> evt)
      {
         _data.Value = evt.newValue;
         _isChanged = true;

         UpdateHeight();

         if (!_data.DeleteMarker)
         {
            ApplyBorderStyle(ChangedBorderColor);
         }
      }

      private void UpdateHeight()
      {
         if (_valueField.resolvedStyle.height > 0)
         {
            _root.style.height = _valueField.resolvedStyle.height + 10;
         }
         _root.MarkDirtyRepaint();
      }

      private void ApplyBorderStyle(Color color)
      {
         _valueField.style.borderBottomWidth = 2;
         _valueField.style.borderLeftWidth = 2;
         _valueField.style.borderRightWidth = 2;
         _valueField.style.borderTopWidth = 2;
         _valueField.style.borderBottomColor = color;
         _valueField.style.borderLeftColor = color;
         _valueField.style.borderRightColor = color;
         _valueField.style.borderTopColor = color;
      }

      private void ClearBorderStyle()
      {
         _valueField.style.borderBottomWidth = 0;
         _valueField.style.borderLeftWidth = 0;
         _valueField.style.borderRightWidth = 0;
         _valueField.style.borderTopWidth = 0;
         _valueField.style.borderBottomColor = TransparentColor;
         _valueField.style.borderLeftColor = TransparentColor;
         _valueField.style.borderRightColor = TransparentColor;
         _valueField.style.borderTopColor = TransparentColor;
      }

      /// <summary>
      /// Handles copy key button click - copies the preference key to the clipboard.
      /// </summary>
      private void OnCopyKeyClicked()
      {
         EditorGUIUtility.systemCopyBuffer = _keyLabel.text;
         ProvideCopyFeedback(_copyKeyButton);
      }

      /// <summary>
      /// Handles copy value button click - copies the preference value to the clipboard.
      /// </summary>
      private void OnCopyValueClicked()
      {
         EditorGUIUtility.systemCopyBuffer = _valueField.value;
         ProvideCopyFeedback(_copyValueButton);
      }

      /// <summary>
      /// Provides brief visual feedback when a copy operation succeeds.
      /// </summary>
      /// <param name="button">The button that was clicked.</param>
      private void ProvideCopyFeedback(Button button)
      {
         if (button == null) return;

         // Store original color
         var originalColor = button.style.backgroundColor.value;

         // Apply feedback color
         button.style.backgroundColor = CopyFeedbackColor;

         // Reset after brief delay (300ms)
         EditorApplication.delayCall += () =>
         {
            if (button != null)
            {
               button.style.backgroundColor = originalColor;
            }
         };
      }
   }
}
