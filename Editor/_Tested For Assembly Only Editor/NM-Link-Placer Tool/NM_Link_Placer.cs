using UnityEngine;
using Unity.AI.Navigation;

namespace TutaX1Tool
{

    #if UNITY_EDITOR
    using UnityEditor;

    [RequireComponent(typeof(NavMeshSurface))]
    public class NM_Link_Placer : MonoBehaviour
    {
        [HideInInspector] public bool createLinkMode = false;
        public GameObject navmeshLinkPrefab;
        public GameObject parent;

        [Header ("NM Link Settings")]
        public float width = 3f;
        public int costModifier = -1;
        public bool bidirectional = true;
        public bool autoUpdatePosition = true;
        public int agentTypeID = 0;
        public int AreaTypeID = 0;

        private Vector3 startPos = Vector3.zero;

        void Awake()
        {
            if (parent == null)
                parent = gameObject;
        }

        public void ToggleCreateLinkMode()
        {
            createLinkMode = !createLinkMode;
            startPos = Vector3.zero;
        }
        
        public void SetCreateLinkMode(bool value)
        {
            createLinkMode = value;
            startPos = Vector3.zero;
        }

        public GameObject CreateLink(Event e)
        {
            if (createLinkMode)
            {
                if (startPos == Vector3.zero) //first mouse click
                {
                    if (MouseToWorldPos(e, out Vector3 worldPos))
                    {
                        //create temporary nmlink start-position info
                        startPos = worldPos + Vector3.up * .05f;
                    }
                }
                else 
                {
                    if (MouseToWorldPos(e, out Vector3 worldPos)) //second mouse click
                    {
                        //instantiate nmlink prefab with start and end position info
                        GameObject nmLinkObj = Instantiate(
                                navmeshLinkPrefab,
                                startPos,
                                Quaternion.Euler(0f, 0f, 0f));
                        nmLinkObj.transform.SetParent(parent.transform);
                        var nmLink = nmLinkObj.GetComponent<NavMeshLink>();
                        nmLink.startPoint = Vector3.zero;
                        nmLink.endPoint = nmLinkObj.transform.InverseTransformPoint(worldPos) + Vector3.up * .05f;

                        //addition settings
                        nmLink.width = width;
                        nmLink.costModifier = costModifier;
                        nmLink.bidirectional = bidirectional;
                        nmLink.autoUpdate = autoUpdatePosition;
                        nmLink.area = AreaTypeID;
                        nmLink.agentTypeID = agentTypeID;

                        //reset startPos info for next nmLink
                        startPos = Vector3.zero;
                        return nmLinkObj;
                    }
                }
            }
            else 
            {
                startPos = Vector3.zero; //cancel any half-made navmesh links
            }
            return null;
        }

        private bool MouseToWorldPos(Event e, out Vector3 worldPos)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hitInfo;
            worldPos = Vector3.zero;

            bool hitSuccess = Physics.Raycast(worldRay, out hitInfo, Mathf.Infinity);
            if (hitSuccess)
                worldPos = hitInfo.point;

            return hitSuccess;
        }
    }
    #endif


    #if UNITY_EDITOR

    [CustomEditor( typeof( NM_Link_Placer ) )]
    public class NM_Link_Placer_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Link Mode")) //button to toggle create-link mode
            {
                ((NM_Link_Placer) target).ToggleCreateLinkMode();
            }
            GUILayout.Toggle(((NM_Link_Placer) target).createLinkMode, "");
            GUILayout.EndHorizontal();
        }

        void OnSceneGUI()
        {
            NM_Link_Placer t = ((NM_Link_Placer) target);
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && e.isMouse)
                    {
                        Undo.RecordObject(target, "Create nmLink");
                        GameObject createdNMLink = t.CreateLink(e); 
                        if (createdNMLink) Undo.RegisterCreatedObjectUndo(createdNMLink, "Create new navmesh link"); 
                    }
                    break;

                case EventType.KeyUp:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        t.SetCreateLinkMode(false); //hotkey to disable createLinkMode
                    }
                    if (e.keyCode == KeyCode.L && e.control)
                    {
                        t.SetCreateLinkMode(true); //hotkey to enable createLinkMode
                    }
                    break;
            }
        }
    }
 
    #endif
}

