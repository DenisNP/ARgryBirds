﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An Edge is a boundary between two Polygons. We're going to be working with loops of Edges, so
// each Edge will have a Polygon that's inside the loop and a Polygon that's outside the loop.
// We also want to Split apart the inner and outer Polygons so that they no longer share the same
// vertices. This means the Edge will need to keep track of what the outer Polygon's vertices are
// along its border with the inner Polygon, and what the inner Polygon's vertices are for that
// same border.

public class Edge
{
    public Polygon m_InnerPoly; //The Poly that's inside the Edge. The one we'll be extruding or insetting.
    public Polygon m_OuterPoly; //The Poly that's outside the Edge. We'll be leaving this one alone.

    public List<int> m_OuterVerts; //The vertices along this edge, according to the Outer poly.
    public List<int> m_InnerVerts; //The vertices along this edge, according to the Inner poly.

    public int m_InwardDirectionVertex; //The third vertex of the inner polygon. That is, the one
                                        //that doesn't touch this edge.

    public Edge(Polygon inner_poly, Polygon outer_poly)
    {
        m_InnerPoly  = inner_poly;
        m_OuterPoly  = outer_poly;
        m_OuterVerts = new List<int>(2);
        m_InnerVerts = new List<int>(2);

        // Examine all three of the inner poly's vertices. Add the vertices that it shares with the
        // outer poly to the m_InnerVerts list. We also make a note of which vertex wasn't on the edge
        // and store it for later in m_InwardDirectionVertex.

        foreach (int vertex in inner_poly.m_Vertices)
        {
            if (outer_poly.m_Vertices.Contains(vertex))
                m_InnerVerts.Add(vertex);
            else
                m_InwardDirectionVertex = vertex;
        }

        // Calculate the 'inward direction', a vector that goes from the midpoint of the edge, to the third vertex on
        // the inner poly (the vertex that isn't part of the edge). This will come in handy later if we want to push
        // vertices directly away from the edge.

        // For consistency, we want the 'winding order' of the edge to be the same as that of the inner
        // polygon. So the vertices in the edge are stored in the same order that you would encounter them if
        // you were walking clockwise around the polygon. That means the pair of edge vertices will be:
        // [1st inner poly vertex, 2nd inner poly vertex] or
        // [2nd inner poly vertex, 3rd inner poly vertex] or
        // [3rd inner poly vertex, 1st inner poly vertex]
        //
        // The formula above will give us [1st inner poly vertex, 3rd inner poly vertex] though, so
        // we check for that situation and reverse the vertices.

        if(m_InnerVerts[0] == inner_poly.m_Vertices[0] && m_InnerVerts[1] == inner_poly.m_Vertices[2])
        {
            int temp = m_InnerVerts[0];
            m_InnerVerts[0] = m_InnerVerts[1];
            m_InnerVerts[1] = temp;
        }

        // No manipulations have happened yet, so the outer and inner Polygons still share the same vertices.
        // We can instantiate m_OuterVerts as a copy of m_InnerVerts.

        m_OuterVerts = new List<int>(m_InnerVerts);
    }
}

// EdgeSet is a collection of unique edges. Basically it's a HashSet, but we have
// extra convenience functions that we'd like to include in it.

public class EdgeSet : HashSet<Edge>
{
    // Split - Given a list of original vertex indices and a list of replacements,
    //         update m_InnerVerts to use the new replacement vertices.

    public void Split(List<int> oldVertices, List<int> newVertices)
    {
        foreach(Edge edge in this)
        {
            for(int i = 0; i < 2; i++)
            {
                edge.m_InnerVerts[i] = newVertices[oldVertices.IndexOf(edge.m_OuterVerts[i])];
            }
        }
    }

    // GetUniqueVertices - Get a list of all the vertices referenced
    // in this edge loop, with no duplicates.

    public List<int> GetUniqueVertices()
    {
        List<int> vertices = new List<int>();

        foreach (Edge edge in this)
        {
            foreach (int vert in edge.m_OuterVerts)
            {
                if (!vertices.Contains(vert))
                    vertices.Add(vert);
            }
        }
        return vertices;
    }

    // GetInwardDirections - For each vertex on this edge, calculate the direction that
    // points most deeply inwards. That's the average of the inward direction of each edge
    // that the vertex appears on.

    public Dictionary<int, Vector3> GetInwardDirections(List<Vector3> vertexPositions)
    {
        var inwardDirections = new Dictionary<int, Vector3>();
        var numContributions = new Dictionary<int, int>();

        foreach (Edge edge in this)
        {
            Vector3 innerVertexPosition = vertexPositions[edge.m_InwardDirectionVertex];

            Vector3 edgePosA   = vertexPositions[edge.m_InnerVerts[0]];
            Vector3 edgePosB   = vertexPositions[edge.m_InnerVerts[1]];
            Vector3 edgeCenter = Vector3.Lerp(edgePosA, edgePosB, 0.5f);

            Vector3 innerVector = (innerVertexPosition - edgeCenter).normalized;

            for(int i = 0; i < 2; i++)
            {
                int edgeVertex = edge.m_InnerVerts[i];

                if (inwardDirections.ContainsKey(edgeVertex))
                {
                    inwardDirections[edgeVertex] += innerVector;
                    numContributions[edgeVertex]++;
                }
                else
                {
                    inwardDirections.Add(edgeVertex, innerVector);
                    numContributions.Add(edgeVertex, 1);
                }
            }
        }

        // Now we average the contributions that each vertex received, and we can return the result.

        foreach (KeyValuePair<int, int> kvp in numContributions)
        {
            int vertexIndex               = kvp.Key;
            int contributionsToThisVertex = kvp.Value;
            inwardDirections[vertexIndex] = (inwardDirections[vertexIndex] / contributionsToThisVertex).normalized;
        }

        return inwardDirections;
    }
}