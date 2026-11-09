using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Anchors;
using System.Collections.Generic;
using System.Linq;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Algorithms;

/// <summary>Layout helpers that rewire links; they change the diagram's links but never its nodes.</summary>
public static class LinksReconnectionAlgorithms
{
    /// <summary>
    /// Moves both ends of every link to the closest pair of ports on the same two nodes (or groups), measured between the ports'
    /// <c>Position</c>s. Only links attached at both ends with a <c>SinglePortAnchor</c> are considered; the others are skipped.
    /// Ports that have not been rendered and measured yet all sit at the origin, so call it after the diagram has rendered.
    /// </summary>
    public static void ReconnectLinksToClosestPorts(this Diagram diagram)
    {
        // Only refresh ports once
        var modelsToRefresh = new HashSet<Model>();

        foreach (var link in diagram.Links.ToArray())
        {
            if (link.Source is not SinglePortAnchor spa1 || link.Target is not SinglePortAnchor spa2)
                continue;

            var sourcePorts = spa1.Port.Parent.Ports;
            var targetPorts = spa2.Port.Parent.Ports;

            // Find the ports with minimal distance
            var minDistance = double.MaxValue;
            var minSourcePort = spa1.Port;
            var minTargetPort = spa2.Port;
            foreach (var sourcePort in sourcePorts)
            {
                foreach (var targetPort in targetPorts)
                {
                    var distance = sourcePort.Position.DistanceTo(targetPort.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minSourcePort = sourcePort;
                        minTargetPort = targetPort;
                    }
                }
            }

            // Reconnect
            if (spa1.Port != minSourcePort)
            {
                modelsToRefresh.Add(spa1.Port);
                modelsToRefresh.Add(minSourcePort);
                link.SetSource(new SinglePortAnchor(minSourcePort));
                modelsToRefresh.Add(link);
            }

            if (spa2.Port != minTargetPort)
            {
                modelsToRefresh.Add(spa2.Port);
                modelsToRefresh.Add(minTargetPort);
                link.SetTarget(new SinglePortAnchor(minTargetPort));
                modelsToRefresh.Add(link);
            }
        }

        foreach (var model in modelsToRefresh)
        {
            model.Refresh();
        }
    }
}
