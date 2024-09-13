using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using BundleSystem;

public class AssetGroupEditorWindow : EditorWindow
{
    private AssetbundleBuildSettings settings;
    private Vector2 scrollPosition;
    private BundleSetting editingGroup = null;
    private bool isRenaming = false;
    private string newGroupName = "";
    private Rect editingGroupRect;
    private BundleSetting dropTargetGroup = null;
    private Dictionary<BundleSetting, Rect> groupRects = new Dictionary<BundleSetting, Rect>();
    private GenericMenu contextMenu;
    private GenericMenu toolsMenu;
    private GenericMenu newMenu;

    private string searchQuery = ""; // Search query for filtering groups

    [MenuItem("Window/Asset Group Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<AssetGroupEditorWindow>("Asset Group Editor");
        window.minSize = new Vector2(300, 200);
        window.maxSize = new Vector2(600, 600);
        window.Show();
    }

    private void OnEnable()
    {
        settings = AssetbundleBuildSettings.EditorInstance;
        if (settings == null)
        {
            Debug.LogError("AssetbundleBuildSettings instance not found.");
            Close();
        }

        // Initialize dropdown menus
        toolsMenu = new GenericMenu();
        toolsMenu.AddItem(new GUIContent("Option 1"), false, () => Debug.Log("Option 1 selected"));
        toolsMenu.AddItem(new GUIContent("Option 2"), false, () => Debug.Log("Option 2 selected"));

        newMenu = new GenericMenu();
        newMenu.AddItem(new GUIContent("Add Group"), false, AddGroup);
    }

    private void OnGUI()
    {
        if (settings == null) return;

        DrawToolbar(); // Draw the toolbar with dropdown buttons and search field

        DrawGroups();
        HandleRightClickMenu();
        HandleDragAndDrop();
        HandleEditing();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown))
        {
            toolsMenu.ShowAsContext();
        }

        if (GUILayout.Button("New", EditorStyles.toolbarDropDown))
        {
            newMenu.ShowAsContext();
        }

        GUILayout.Space(10);

        // Search field
        searchQuery = GUILayout.TextField(searchQuery, "ToolbarSearchTextField", GUILayout.Width(200));

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private Dictionary<BundleSetting, bool> groupExpansionStates = new Dictionary<BundleSetting, bool>();

    private void DrawGroups()
    {
        EditorGUILayout.LabelField("Asset Groups", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Group rects and filtered groups
        groupRects.Clear();

        foreach (var group in settings.BundleSettings)
        {
            // Filter groups based on the search query
            if (string.IsNullOrEmpty(searchQuery) || AssetInGroup(group, searchQuery))
            {
                Rect groupRect = DrawGroup(group);
                groupRects[group] = groupRect;
            }
        }

        EditorGUILayout.EndScrollView();
    }
    
    private bool AssetInGroup(BundleSetting group, string searchQuery)
    {
        foreach (var assetRef in group.AssetReferences)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetRef.guid);
            assetPath = assetPath.Substring(assetPath.LastIndexOf('/') + 1);
            if (assetPath.ToLower().Contains(searchQuery.ToLower()))
            {
                return true;
            }
        }

        return false;
    }
    private Rect DrawGroup(BundleSetting group)
    {
        if (!groupExpansionStates.ContainsKey(group))
        {
            groupExpansionStates[group] = true;
        }

        EditorGUILayout.BeginVertical("box");

        if (!isRenaming || editingGroup != group)
        {
            bool isExpanded = EditorGUILayout.Foldout(groupExpansionStates[group], group.BundleName, true);

            if (EditorGUI.EndChangeCheck())
            {
                groupExpansionStates[group] = isExpanded;
            }

            Rect groupRect = GUILayoutUtility.GetLastRect();
            Event evt = Event.current;
            if (evt.type == EventType.ContextClick && groupRect.Contains(evt.mousePosition))
            {
                ShowContextMenu(group, evt.mousePosition);
                evt.Use();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            newGroupName = EditorGUILayout.TextField(newGroupName);
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && !GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                EndEditingGroup();
            }
        }

        if (groupExpansionStates[group])
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();
            bool includeInPlayer = EditorGUILayout.Toggle("Include in Player", group.IncludedInPlayer);
            if (EditorGUI.EndChangeCheck())
            {
                group.IncludedInPlayer = includeInPlayer;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            foreach (var assetRef in group.AssetReferences)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetRef.guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
        return groupRects[group] = GUILayoutUtility.GetLastRect();
    }

    private void ShowContextMenu(BundleSetting group, Vector2 mousePosition)
    {
        contextMenu = new GenericMenu();
        contextMenu.AddItem(new GUIContent("Rename"), false, () => StartRenamingGroup(group));
        contextMenu.AddItem(new GUIContent("Remove Group"), false, () => RemoveGroup(group));
        contextMenu.ShowAsContext();
    }

    private void HandleRightClickMenu()
    {
        Event evt = Event.current;
        if (evt.type == EventType.ContextClick)
        {
            Vector2 mousePos = evt.mousePosition;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Group"), false, AddGroup);
            menu.ShowAsContext();
            evt.Use();
        }
    }

    private void StartRenamingGroup(BundleSetting group)
    {
        if (isRenaming)
        {
            EndEditingGroup();
        }

        isRenaming = true;
        editingGroup = group;
        newGroupName = group.BundleName;
        editingGroupRect = groupRects[group];
        GUI.FocusControl(null);
        Repaint();
    }

    private void EndEditingGroup()
    {
        if (editingGroup != null)
        {
            editingGroup.BundleName = newGroupName;
            editingGroup = null;
            newGroupName = "";
            isRenaming = false;
            Repaint();
        }
    }

    private void RemoveGroup(BundleSetting group)
    {
        settings.BundleSettings.Remove(group);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private void HandleEditing()
    {
        if (isRenaming && editingGroup != null)
        {
            // TextField already handled in DrawGroup, so just check for focus loss
            if (Event.current.type == EventType.MouseDown)
            {
                // Check if mouse click is outside the text field and editingGroupRect
                if (!editingGroupRect.Contains(Event.current.mousePosition) && !GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    EndEditingGroup();
                }
            }

            // End editing on Enter key press
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                EndEditingGroup();
            }
        }

        // Check if mouse click is outside the search field
        if (Event.current.type == EventType.MouseDown)
        {
            Debug.Log("Mouse down");
            Rect searchFieldRect = GUILayoutUtility.GetLastRect();
            if (!searchFieldRect.Contains(Event.current.mousePosition))
            {
                GUI.FocusControl(null); // Deselect search field
                searchQuery = ""; // Clear search query
                Repaint(); // Repaint to update the UI
            }
        }
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (evt.type == EventType.DragUpdated)
            {
                bool isDraggingOverGroup = false;

                foreach (var group in groupRects)
                {
                    Rect groupRect = group.Value;
                    if (groupRect.Contains(evt.mousePosition))
                    {
                        dropTargetGroup = group.Key;
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        isDraggingOverGroup = true;
                        break;
                    }
                }

                if (!isDraggingOverGroup)
                {
                    dropTargetGroup = null;
                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                }
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (dropTargetGroup != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(draggedObject);
                        string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

                        var assetRef = new AssetReference { guid = assetGUID };
                        dropTargetGroup.AssetReferences.Add(assetRef);
                    
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        Repaint();
                    }
                }

                evt.Use();
            }
        }
    }

    private void AddGroup()
    {
        string newGroupName = "New Group " + (settings.BundleSettings.Count + 1);
        settings.BundleSettings.Add(new BundleSetting { BundleName = newGroupName });
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}
