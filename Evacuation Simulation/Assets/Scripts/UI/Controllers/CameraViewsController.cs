using PedestrianSimulation.Import.Speckle;
using PedestrianSimulation.UI.Elements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PedestrianSimulation.UI.Controllers
{
    [AddComponentMenu("Simulation/UI/Camera Views Controller"), DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class CameraViewsController : MonoBehaviour
    {

        private CameraViewsElement viewElement;
        private VisualElement documentRoot;

        private List<GameObject> availableCameraGroups;

        private void Start()
        {
            UIDocument document = GetComponent<UIDocument>();
            documentRoot = document.rootVisualElement;
            viewElement = documentRoot.Q<CameraViewsElement>();
            if (viewElement != null)
            {
                viewElement.CameraChangeEvent += ChangeCamera;

                ImportManager i = ImportManager.Instance;
                if (i != null)
                {
                    i.OnStreamReceived += CameraUpdate;
                    i.OnReceiverRemove += CameraUpdate;
                    i.OnStreamVisibilityChange += CameraUpdate;
                    i.OnReceiverUpdate += CameraUpdate;
                }

                InitialiseUI();
            }
            else
            {
                Debug.LogWarning($"{this} could not find a {typeof(CameraViewsElement)} in {document}");
            }
        }
        private void CameraUpdate<A, B, C>(A a = default, B b = default, C c = default) => InitialiseUI();
        private void CameraUpdate<A, B>(A a = default, B b = default) => InitialiseUI();
        private void InitialiseUI()
        {
            viewElement.Clear();
            availableCameraGroups = new List<GameObject>();

            //Dynamic cameras
            foreach (GameObject cameraParent in GameObject.FindGameObjectsWithTag("Cameras"))
            {
                foreach (Transform child in cameraParent.transform)
                {
                    if (child.GetComponentInChildren<Camera>(true) != null || child.GetComponentInChildren<Camera>(true) != null)
                    {
                        availableCameraGroups.Add(child.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"Camera child {child.gameObject} missing {typeof(Camera)} component in self or children", child.gameObject);
                    }
                }
            }

            //Static Cameras
            foreach (GameObject environment in GameObject.FindGameObjectsWithTag("Environment"))
            {
                foreach (Camera cam in environment.GetComponentsInChildren<Camera>(true))
                {
                    availableCameraGroups.Add(cam.gameObject);
                }
            }


            //Create UI for all cameras
            foreach (GameObject camGroup in availableCameraGroups)
            {
                if (!camGroup.transform.parent.gameObject.activeInHierarchy) continue;
                
                viewElement.AddElement(new CameraViewViewModel() { gameObject = camGroup, name = camGroup.name });
                CameraControlsHelper.CreateControls(camGroup, documentRoot.Q("top"));
            }
            
        }

        private void ChangeCamera(GameObject cam)
        {
            foreach (GameObject c in availableCameraGroups)
            {
                c.SetActive(false);
            }
            cam.SetActive(true);
        }
    }
}
