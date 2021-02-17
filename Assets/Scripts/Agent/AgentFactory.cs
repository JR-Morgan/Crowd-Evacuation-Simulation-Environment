using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentFactory : MonoBehaviour
{
    private static AgentFactory _instance;
    public static AgentFactory Instance { get => _instance; }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }



    [SerializeField]
    private GameObject AgentPrefab;

    [SerializeField]
    private int numberOfAgents;
    [SerializeField]
    private GameObject env;
    [SerializeField]
    private Transform goal;

    private void Start()
    {
        SpawnAllAgents();
    }

    public void SpawnAllAgents() => SpawnAllAgents(numberOfAgents, env);

    public void SpawnAllAgents(int numberOfAgents, GameObject environmentModel, int tries = 100, float distance = 1f)
    {
        Bounds bounds = CalculateLocalBounds(environmentModel);

        for (int i = 0; i < numberOfAgents; i++)
        {
            GameObject agent = Instantiate(AgentPrefab, transform);
            NavMeshAgent navAgent = agent.transform.GetComponent<NavMeshAgent>();
            AgentBehaviour agentBehaviour = agent.transform.GetComponent<AgentBehaviour>();

            bool failed = true;
            for (int j = 0; j < tries; j++)
            {

                Vector3 position = GetRandomPointOnNavMesh(bounds, environmentModel.transform, distance);
                if (!position.Equals(Vector3.positiveInfinity))
                {
                    navAgent.Warp(position);
                    if(agentBehaviour.CalcualtePath(goal.position))
                    {
                        failed = false;
                        break;
                    }
                }
                
            }
            if (failed)
            {
                Destroy(agent);
                Debug.LogWarning($"Failed to instantiate agent in a valid location after {tries} tries.");
            }

        }
        
    }

    private static Bounds CalculateLocalBounds(GameObject model)
    {
        Quaternion currentRotation = model.transform.rotation;
        model.transform.rotation = Quaternion.Euler(Vector3.zero);

        Bounds bounds = new Bounds(model.transform.position, Vector3.one);

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }

        Vector3 localCenter = bounds.center - model.transform.position;
        bounds.center = localCenter;

        model.transform.rotation = currentRotation;


        return bounds;
    }


    private static Vector3 GetRandomPointOnNavMesh(in Bounds bounds, Transform transform, float distance)
    {
        Vector3 randomPoint =  new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        randomPoint = transform.TransformPoint(randomPoint);

        //randomPoint += Random.insideUnitSphere * distance;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, distance, NavMesh.AllAreas))
        {
            return hit.position + Vector3.up ;
        }

        return Vector3.positiveInfinity;
    }

}
