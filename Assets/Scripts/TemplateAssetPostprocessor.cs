/*
 TODO:
 Template Not on root object:
                            - Needs to create a Prefab for what ever gameobject it is placed on, or maybe just its children? (NOT IF ON ROOT)
                            - Needs to add a PlaceHolder Component, linking to that prefab
                            - Evaluate These things on apply?

PlaceHolder Not on root object
                            - Needs to handle apply being pressed

  Only one template coponent
  placeholder requires template

  Validation of PrefabObject
 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[InitializeOnLoad]

public class TemplateAssetPostprocessor : AssetPostprocessor
{

    static Type T_GameObjectInspector;
    static FieldInfo F_DragObject, F_IgnoreRayCast;
    static bool initialised = false;
    GameObject CachedObject;

    public TemplateAssetPostprocessor()
    {
        if (!initialised)
        {
            Init();
        }
    }

    private void Init()
    {
        T_GameObjectInspector = GetHidenType(typeof(Editor), "GameObjectInspector");
        F_DragObject = T_GameObjectInspector.GetField("dragObject");
        var t = typeof(HandleUtility);
        F_IgnoreRayCast = t.GetField("ignoreRaySnapObjects", BindingFlags.Static | BindingFlags.NonPublic);

        SceneView.onSceneGUIDelegate -= SceneViewDragAndDrop;
        SceneView.onSceneGUIDelegate += SceneViewDragAndDrop;

        initialised = true;
    }
    private void SceneViewDragAndDrop(SceneView sceneView)
    {
        var current = Event.current;


        if (current.type == EventType.Repaint || current.type == EventType.Layout) return;

        if (UnityEngine.Event.current.type == EventType.DragPerform)
        {
            CachedObject.hideFlags = HideFlags.None;
            Selection.activeObject = CachedObject;
            CachedObject = null;
            return;

        }
        var references = DragAndDrop.objectReferences;

        if (references.Length > 0 && UnityEngine.Event.current.type == EventType.DragUpdated)//only check relevant drops
        {
            var go = (GameObject)F_DragObject.GetValue(null); //Get the currently dragged objects
            if (go == null)   
            {
                return;//Dragged object hasnt been created yet
            }
            if (CachedObject == null) //Replace the Dragged object, but we only want to do this once
            {
                CachedObject = SliceAndCreate(go);
                CachedObject.hideFlags = HideFlags.HideInHierarchy;
                F_DragObject.SetValue(null, CachedObject);
                F_IgnoreRayCast.SetValue(null, CachedObject.GetComponentsInChildren<Transform>());
            }
          

        }
    }
    static List<UnityEngine.Object> UsedPlaceHolders = new List<UnityEngine.Object>();
    private static GameObject SliceAndCreate(GameObject go)
    {
        var IsTemplatePrefab = go.GetComponent<Template>() != null ? true : false;//Check if it needs splitting from its parent 
   
        if (IsTemplatePrefab)
        {

            var obj = UnityEngine.Object.Instantiate(go);

            obj.transform.parent = go.transform.parent;
            obj.transform.localPosition = go.transform.localPosition;
            obj.transform.localScale = go.transform.localScale;
            obj.transform.localRotation = go.transform.localRotation;
            
            obj.name = go.name; 
            UnityEngine.Object.DestroyImmediate(go, false);

            DragAndDrop.AcceptDrag();





            var PlaceHolders = obj.GetComponents<PlaceHolder>();
            var HasPlaceHolder = PlaceHolders != null ? true : false;//Check if it has a placeholder 

            if (HasPlaceHolder)
            {
                var root = PrefabUtility.FindPrefabRoot(obj);
                if (UsedPlaceHolders.Contains(root))
                {
                    UsedPlaceHolders.Add(root);

                }
                foreach (var placeholder in PlaceHolders)
                {
                    if (placeholder.Prefab == null)
                    {
                        continue;
                    }
                    var parent = PrefabUtility.FindPrefabRoot(placeholder.Prefab);
                    if (UsedPlaceHolders.Contains(parent))
                    {
                        Debug.LogError("Infinate Recurrsion Detected - " + root.name + " has already been created by this object.");
                        UnityEngine.Object.DestroyImmediate(obj, false);//Not really needed. But makes it more obvious why the placeholder it didnt get created
                        continue;
                    }
                    else
                    {
                        UsedPlaceHolders.Add(parent);
                        var prefab = (GameObject)PrefabUtility.InstantiatePrefab(placeholder.Prefab);
                        placeholder.Created = prefab;
                        var uniqueNameForSibling = GameObjectUtility.GetUniqueNameForSibling(obj.transform, prefab.name);

                        prefab.name = uniqueNameForSibling;
                        prefab.transform.parent = obj.transform;

                        prefab.transform.localPosition = Vector3.zero;
                        SliceAndCreate(prefab);
                    }
           

                }



            }

            UsedPlaceHolders.Clear();


            return obj;

        }
        else
            return go;


    }

    public static System.Type GetHidenType(System.Type aBaseClass, string TypeName)
    {
        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var A in AS)
        {
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                if (T.IsSubclassOf(aBaseClass) && T.Name == TypeName)
                    return T;
            }
        }
        return null;
    }

    IEnumerator Wait(GameObject obj)
    {
        yield return null;
        ((GameObject)obj).transform.position = ((GameObject)Selection.activeObject).transform.position;

    }

}
