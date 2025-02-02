using System.Collections.Generic;
using System.Linq;
using Results_Core;
using AgentState = PedestrianSimulation.Agent.AgentState;
using ResultsAgentState = Results_Core.AgentState;
using Vector3 = UnityEngine.Vector3;
using ResultsVector3 = Results_Core.Vector3;

namespace PedestrianSimulation.Results
{
    public static class ResultsHelper
    {
        public static SimulationResults GenerateResults(int numberOfAgents, float realTimeToExecute, float timeToEvacuate, IEnumerable<IEnumerable<AgentState>> agentStates)
        {
            var evacuationTime = agentStates.SelectMany(a => a)
                .Where(a => !a.active)
                .GroupBy(a => a.id)
                .Select(g => g.OrderBy(a => a.time).First());

            float meanTimeToEvacuate = evacuationTime.Select(a => a.time).Average();
            
            
            return new SimulationResults(
                numberOfAgents: numberOfAgents,
                realTimeToExecute: realTimeToExecute ,
                timeToEvacuate: timeToEvacuate,
                meanTimeToEvacuate: meanTimeToEvacuate,
                timeData: ProcessTimeData(agentStates));

            static TimeData[] ProcessTimeData(IEnumerable<IEnumerable<AgentState>> agentStates)
            {
                return agentStates.Select(a => new TimeData(a.Select(b => b.AsResult()).ToArray())).ToArray();
            }
        }

        
        #region AsResult conversion methods
        private static ResultsAgentState AsResult(this AgentState state)
        {
            return new ResultsAgentState
            {
                id = state.id,
                time = state.time,
                active = state.active,
                radius = state.radius,
                rotation = state.rotation.eulerAngles.AsResult(),
                desiredSpeed = state.desiredSpeed,
                goal = state.goal.AsResult(),
                position =  state.position.AsResult(),
                velocity = state.velocity.AsResult(),
            };
        }
        
        private static ResultsVector3 AsResult(this Vector3 vector)
        {
            return new ResultsVector3()
            {
                x = vector.x,
                y = vector.y,
                z = vector.z,
            };
        }
        #endregion
        
    }
}
