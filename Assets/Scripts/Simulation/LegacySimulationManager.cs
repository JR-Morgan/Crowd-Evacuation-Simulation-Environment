using PedestrianSimulation.Agent;
using PedestrianSimulation.Simulation.Initialisation;
using PedestrianSimulation.Visualisation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace PedestrianSimulation.Simulation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshSurface))]
    public class LegacySimulationManager : Singleton<LegacySimulationManager>, ISimulationManager<LegacyPedestrianAgent>
    {

        #region Serialized Field
        [Header("Asset References")]
        [SerializeField]
        private Material heatmapMaterial;
        [Header("Prefab References")]
        [SerializeField]
        private GameObject AgentPrefab;

        #endregion;


        #region Events
        [SerializeField]
        private UnityEvent _onSimulationStart;
        public UnityEvent OnSimulationStart => _onSimulationStart;

        [SerializeField]
        private UnityEvent _onSimulationStop;
        public UnityEvent OnSimulationStop => _onSimulationStop;
        #endregion

        private GameObject visualSurface;
        private NavMeshSurface navMeshSurface;

        public bool IsRunning { get; private set; }
        public bool HasGenerated => Agents != null;
        public IList<LegacyPedestrianAgent> Agents { get; private set; } = null;

        protected override void Awake()
        {
            base.Awake();
            navMeshSurface = GetComponent<NavMeshSurface>();
            Initialise();
        }


        /// <summary>
        /// Cancels the running simulation
        /// </summary>
        /// <returns><c>false</c> if the simulation was not running; otherwise <c>true</c></returns>
        public bool CancelSimulation()
        {
            if (IsRunning)
            {
                Initialise();
                OnSimulationStop.Invoke();
            }
            return IsRunning;
        }

        /// <summary>
        /// (Re)Initialises the <see cref="LegacySimulationManager"/>. Will remove all <see cref="LegacyPedestrianAgent"/>s in the scene.
        /// </summary>
        public void Initialise()
        {
            if (HasGenerated)
            {
                WorldStateManager.Instance.enabled = false;

                foreach (Component agent in Agents)
                {
                    Destroy(agent.gameObject);
                }

                Agents = null;
            }
            IsRunning = false;
        }

        /// <summary>
        /// Attempts to initialise and start a simulation with the given <paramref name="settings"/>
        /// </summary>
        /// <param name="settings">The <see cref="SimulationSettings"/> that are to be setup</param>
        /// <param name="environment">The <see cref="GameObject"/> that represents the environment of agents</param>
        /// <returns><c>true</c> if the simulation was successfully setup; otherwise, <c>false</c></returns>
        public bool RunSimulation(SimulationSettings settings, GameObject environment)
        {
            if (IsRunning)
            {
                Debug.Log("Unable to start simulation as one is already running!", this);
            }
            else
            {
                // 0. Cleanup any data from last run
                if (visualSurface != null) Destroy(visualSurface);

                IsRunning = true;

                // 1. Initialise Random
                Random.InitState(settings.seed);

                // 2. Prepare Environment
                Transform[] c = environment.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in c)
                {
                    t.gameObject.layer = (int)(Mathf.Log((uint)navMeshSurface.layerMask.value, 2));
                }

                // 2. Build NavMesh
                navMeshSurface.BuildNavMesh();

                // 3. Setup Visual Surface
                var parent = GameObject.FindGameObjectWithTag("Visualisations").transform;
                visualSurface = InstantiateVisualSurfaceMesh(heatmapMaterial, parent);

                // 4. Initialise Agents
                Agents = UniformAgentDistribution<LegacyPedestrianAgent>.InstantiateAgents(
                    agentParent: transform,
                    goal: settings.goal,
                    agentPrefab: AgentPrefab,
                    numberOfAgents: settings.numberOfAgents,
                    environmentModel: environment
                    );

                // 5. Enable World State
                WorldStateManager.Instance.enabled = true;

                // 6. Invoke Event
                OnSimulationStart.Invoke();

                Debug.Log("Simulation has started!", this);
            }

            return IsRunning;
        }

        private static GameObject InstantiateVisualSurfaceMesh(Material material, Transform parent = null, string name = "Visual Surface")
        {
            GameObject visualSurfaceGO = new GameObject(name);
            visualSurfaceGO.transform.position = new Vector3(0f, 0.001f, 0f); //Small Y offset to stop Z fighting

            if (parent != null) visualSurfaceGO.transform.parent = parent;

            NavMeshTriangulation navmesh = NavMesh.CalculateTriangulation();
            Mesh mesh = new Mesh
            {
                vertices = navmesh.vertices,
                triangles = navmesh.indices
            };

            //Add Components

            MeshFilter filter = visualSurfaceGO.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = visualSurfaceGO.AddComponent<MeshRenderer>();
            renderer.material = material;

            visualSurfaceGO.AddComponent<VisualSurface>();

            return visualSurfaceGO;

        }


    }
}