using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Anchors;
using System.Collections.Generic;
using System.Linq;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Algorithms;

/// <summary>Layout helpers that rewire links; they change the diagram's links but never its nodes.</summary>
public static class LinksReconnectionAlgorithms
{
    /// <summary>
    /// Moves both ends of every link to the closest pair of ports on the same two nodes (or groups), measured between the ports'
    /// centres (<c>MiddlePosition</c>). Only a pair whose source port is not locked and accepts the target port in
    /// <c>CanAttachTo</c>, and whose target port is not locked, is a candidate. Only links attached at both ends with a
    /// <c>SinglePortAnchor</c> are considered; locked links, and links with an end on a locked port or node, are left alone.
    /// Ports that have not been rendered and measured yet all sit at the origin, so call it after the diagram has rendered.
    /// </summary>
    public static void ReconnectLinksToClosestPorts(this Diagram diagram)
    {
        // Only refresh ports once
        var modelsToRefresh = new HashSet<Model>();

        foreach (var link in diagram.Links.ToArray())
        {
            if (link.Locked || link.Source is not SinglePortAnchor spa1 || link.Target is not SinglePortAnchor spa2)
                continue;

            if (IsLocked(spa1.Port) || IsLocked(spa2.Port))
                continue;

            var sourcePorts = spa1.Port.Parent.Ports;
            var targetPorts = spa2.Port.Parent.Ports;

            // Find the ports with minimal distance
            var minDistance = double.MaxValue;
            var minSourcePort = spa1.Port;
            var minTargetPort = spa2.Port;
            foreach (var sourcePort in sourcePorts)
            {
                if (sourcePort.Locked)
                    continue;

                foreach (var targetPort in targetPorts)
                {
                    if (targetPort.Locked || !sourcePort.CanAttachTo(targetPort))
                        continue;

                    var distance = sourcePort.MiddlePosition.DistanceTo(targetPort.MiddlePosition);
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

    private static bool IsLocked(PortModel port) => port.Locked || port.Parent.Locked;
}
