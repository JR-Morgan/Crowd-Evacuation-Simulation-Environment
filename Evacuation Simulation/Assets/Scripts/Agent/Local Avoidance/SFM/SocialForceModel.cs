﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PedestrianSimulation.Agent.LocalAvoidance.SFM
{
    // This class has been adapted from https://github.com/fawwazbmn/SocialForceModel C++ SFM
    // Licence: BSD 3-Clause License https://github.com/fawwazbmn/SocialForceModel/blob/master/LICENSE
    //
    // With changes inspired by https://github.com/trinhthanhtrung/unity-pedestrian-sim 
    // Licence: MIT License https://github.com/trinhthanhtrung/unity-pedestrian-sim/blob/master/LICENSE
    //
    // With SMF parameters from Moussaïd et al. (2009). https://doi.org/10.1098/rspb.2009.0405
    public static class SocialForceModel
    {
        //TODO benchmark in reference parameters


        public static Vector3 CalculateNextVelocity(AgentState agent, AgentEnvironmentModel model)
        {
            Vector3 drivingForce = DrivingForce(agent);
            Vector3 interactionForce = InteractionForce(agent, model.Neighbours);
            Vector3 obstacleInteractionForce = ObstacleInteractionForce(agent, model.Walls);

            interactionForce.y = 0; //Assumes all agents are vertically oriented
            obstacleInteractionForce.y = 0; //Assumes all walls are vertical oriented (perhaps a less fair assumption than the latter)
            
#if UnityEngine
            Debug.DrawLine(agent.position, agent.position + drivingForce, Color.red);
            Debug.DrawLine(agent.position, agent.position + interactionForce, Color.cyan);
            Debug.DrawLine(agent.position, agent.position + obstacleInteractionForce, Color.yellow);
#endif
            
            return drivingForce +
                   interactionForce +
                   obstacleInteractionForce;
        }
        public static Vector3 DrivingForce(AgentState agent)
        {
            const float relaxationT = 0.54f; // 0.54 Moussaïd and Trịnh Thành Trung

            Vector3 desiredDirection = Vector3.Normalize(agent.goal - agent.position);

            Vector3 drivingForce = ((agent.desiredSpeed * desiredDirection) - agent.velocity) / relaxationT;
            return drivingForce;
        }

        public static Vector3 InteractionForce(AgentState agent, IEnumerable<AgentState> neighbours)
        {
            //const float observableRadiusSquared = 10f * 10f;
            const float lambda = 2f;
            const float gamma = 0.35f;
            const float nPrime = 3f;
            const float n = 2f;
            const float a = 45f; // (47.0 Trịnh Thành Trung) (4.5 * 10 Moussaïd)

            Vector3 interactionForce = Vector3.zero;
            
            foreach (AgentState neighbour in neighbours)
            {
                if (agent.id == neighbour.id) continue;

                //continue if agent is too far (this might be replaced later with a hash grid system)
                Vector3 translationToNeighbour = agent.position - neighbour.position;
                //if (translationToNeighbour.sqrMagnitude > observableRadiusSquared) continue;

                Vector3 directionToNeighbour = Vector3.Normalize(translationToNeighbour);
                Vector3 interactionVector = lambda * (agent.velocity - neighbour.velocity) + directionToNeighbour;

                var B = gamma * interactionVector.magnitude;

                Vector3 interactionDirection = Vector3.Normalize(interactionVector);

                var theta = Vector3.Angle(interactionDirection, directionToNeighbour);


                var K = (int)Mathf.Sign(theta);

                float distanceToNeighbour = translationToNeighbour.magnitude;
                float deceleration = -a * Mathf.Exp(-distanceToNeighbour / B - (nPrime * B * theta) * (nPrime * B * theta));
                float directionalChange = -a * K * Mathf.Exp(-distanceToNeighbour / B - (n * B * theta) * (n * B * theta));

                Vector3 normalInteractionVector = new Vector3(-interactionDirection.z, interactionDirection.y, interactionDirection.x);

                interactionForce += deceleration * interactionDirection + directionalChange * normalInteractionVector;
            }

            return interactionForce;
        }

        public static Vector3 ObstacleInteractionForce(AgentState agent, IEnumerable<Wall> walls)
        {
            //const float repulsionRange = 0.3f;	// Repulsion range based on (Moussaïd et al., 2009)
            const int a = 3;
            const float b = 1f; // (0.8 Trịnh Thành Trung) (0.1 * 10 Moussaïd)

            Vector3 minDistanceVector = Vector3.zero;
            float sqrMinDistance = float.PositiveInfinity;
            
            foreach(Wall wall in walls)
            {
                Vector3 nearestPoint = agent.position - wall.GetNearestPoint(agent.position);
                float sqrDistance = nearestPoint.sqrMagnitude;

                // Store Nearest Wall Distance
                if (sqrDistance < sqrMinDistance)
                {
                    sqrMinDistance = sqrDistance;
                    minDistanceVector = nearestPoint;
                }
            }

            float distanceToNearestObstacle = Mathf.Sqrt(sqrMinDistance) - agent.radius;    // Distance between wall and agent i

            float interactionForce = a * Mathf.Exp(-distanceToNearestObstacle / b);
            minDistanceVector.Normalize();

            return interactionForce * minDistanceVector;
        }

    }
}