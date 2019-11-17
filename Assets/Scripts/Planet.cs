using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Planet : MonoBehaviour
{
    private const float ExtrudeHeight = 0.015f;
    private const float RotationSpeed = 0.01f;
    private const int TotemsCount = 10;
    private const int DisabledTotemsCount = 2;
    private const int StartNaturePercent = 75;
    private const int MaxScore = 1000;
    private const float BackwardsSpeedCoeff = 100f;
    private const float LowerSpeed = 0.01f;
    private const float HigherSpeed = 0.05f;
    private const float LowerAngularSpeed = 0.2f;
    private const float HigherAngularSpeed = 1.5f;
    private readonly TimeSpan generationPeriod = new TimeSpan(0, 0, 0, 0, 750);
    private readonly TimeSpan backwardsGenerationPeriod = new TimeSpan(0, 0, 0, 0, 50);
    private readonly TimeSpan requestsPeriod = new TimeSpan(0, 0, 0, 0, 500);
    private readonly TimeSpan mutationPeriod = new TimeSpan(0, 0, 0, 15);
    private const int MutationValue = 3;
    private const string StateUri = "https://arngry.herokuapp.com";
    private const string ModeUri = "https://arngry.herokuapp.com";

    private const float topCam = 0.33f;
    private const float bottomCam = -0.46f;
    private const float leftCam = -0.53f;
    private const float rightCam = 0.53f;
    private const float zCam = -2f;

    public Text scoreText;
    public SimpleHealthBar healthBar;
    public GameObject[] landObjects;
    public GameObject[] seaObjects;
    public GameObject hitNone;
    public Bird bird;

    public Text winText;
    public Text looseText;
    public Text dangerText;
    public Text genNumberText;

    public Totem totem;
    public Transform Clouds;
    
    public Material m_GroundMaterial;
    public Material m_OceanMaterial;

    public int   m_NumberOfContinents = 5;
    public float m_ContinentSizeMax   = 1.0f;
    public float m_ContinentSizeMin   = 0.1f;

    public int   m_NumberOfHills = 5;
    public float m_HillSizeMax   = 1.0f;
    public float m_HillSizeMin   = 0.1f;

    private int _score = 0;
    private int _visibleScore = 0;
    
    // Internally, the Planet object stores its meshes as a child GameObjects:
    GameObject m_GroundMesh;
    GameObject m_OceanMesh;

    // The subdivided icosahedron that we use to generate our planet is represented as a list
    // of Polygons, and a list of Vertices for those Polygons:
    List<Polygon> m_Polygons;
    List<Vector3> m_Vertices;

    private int _generateNumber = 3;
    private DateTime _lastGenerated = DateTime.MinValue;
    private DateTime _lastRequest = DateTime.MinValue;
    private bool _requestingNow = false;
    private bool _backwards = false;
    private readonly Dictionary<Polygon, (GameObject, bool)> _planted = new Dictionary<Polygon, (GameObject, bool)>();
    private float civRatio = 0f;
    private float totalRotation = 0f;
    private State _lastState = new State();
    private bool _totemsOn = true;
    private long _lastHitTime = 0;
    private string _lastHitType = "";
    private DateTime _lastMutation = DateTime.Now;
    
    private readonly List<(Totem, Vector3)> _totems = new List<(Totem, Vector3)>();

    void Update()
    {
        var diff = DateTime.Now - _lastGenerated;
        
        if (!_backwards)
        {
            transform.Rotate(new Vector3(0, 1, 1), RotationSpeed);
            Clouds.Rotate(new Vector3(0, 1, 0), RotationSpeed * 0.8f);
            totalRotation += RotationSpeed;
            
            if (diff > generationPeriod)
            {
                var repeat = Math.Abs(_generateNumber);
                var genCiv = repeat > 0;
                if (genCiv)
                {
                    var nature = _planted.Where(x => !x.Value.Item2).ToList();
                    var natureCount = nature.Count;
                    while (natureCount-- > 0 && repeat-- > 0)
                    {
                        var randomPoly = nature[Random.Range(0, natureCount)];
                        AddCiv(randomPoly.Key);
                    }
                }
                else
                {
                    var civ = _planted.Where(x => x.Value.Item2).ToList();
                    var civCount = civ.Count;
                    while (civCount-- > 0 && repeat-- > 0)
                    {
                        var randomPoly = civ[Random.Range(0, civCount)];
                        AddNature(randomPoly.Key);
                    }
                }

                civRatio = _planted.Count(x => x.Value.Item2) * 1f / _planted.Count;
                healthBar.UpdateBar(civRatio, 1f);
                _lastGenerated = DateTime.Now;

                if (civRatio > 0.99f || civRatio < 0.01f)
                {
                    looseText.gameObject.SetActive(true);
                    looseText.text = $"GAME OVER\nyour score: {_score}";
                    TurnBackwards(true);
                    _score = 0;
                }
                else if (civRatio >= 0.9f || civRatio <= 0.1f)
                {
                    dangerText.gameObject.SetActive(true);
                    if (_lastState.HasPose(3))
                    {
                        HideWindows();
                        _score = _score / 2;
                        TurnBackwards(true);
                    }
                }
            }

            if (DateTime.Now - _lastMutation > mutationPeriod)
            {
                _lastMutation = DateTime.Now;
                _generateNumber += Math.Sign(_generateNumber) * Random.Range(0, MutationValue + 1);
                SetGenNumberText();
            }
        }
        else
        {
            transform.Rotate(new Vector3(0, 1, 1), -RotationSpeed * BackwardsSpeedCoeff);
            Clouds.Rotate(new Vector3(0, 1, 0), -RotationSpeed * 0.8f * BackwardsSpeedCoeff);
            totalRotation -= RotationSpeed * BackwardsSpeedCoeff;

            if (diff > backwardsGenerationPeriod)
            {
                var repeat = 2;
                if (Math.Abs(civRatio - 0.5f) <= 0.05f)
                {
                    HideWindows();
                    TurnBackwards(false);
                }
                else
                {
                    if (civRatio < 0.5f)
                    {
                        var nature = _planted.Where(x => !x.Value.Item2).ToList();
                        var natureCount = nature.Count;
                        while (natureCount-- > 0 && repeat-- > 0)
                        {
                            var randomPoly = nature[Random.Range(0, natureCount)].Key;
                            AddCiv(randomPoly);
                        }
                    }
                    else
                    {
                        var civ = _planted.Where(x => x.Value.Item2).ToList();
                        var civCount = civ.Count;
                        while(civCount-- > 0 && repeat-- > 0)
                        {
                            var randomPoly = civ[Random.Range(0, civCount)].Key;
                            AddNature(randomPoly);
                        }
                    }
                }
                
                civRatio = _planted.Count(x => x.Value.Item2) * 1f / _planted.Count;
                healthBar.UpdateBar(civRatio, 1f);
                _lastGenerated = DateTime.Now;
            }
        }

        if (!_requestingNow && DateTime.Now - _lastRequest > requestsPeriod)
        {
            _requestingNow = true;
            _lastRequest = DateTime.Now;
            StartCoroutine(GetState());
        }

        if (_lastState.HasPose(1))
        {
            _totemsOn = true;
            foreach (var (tt, _) in _totems)
            {
                if (tt.Type == "civ")
                {
                    tt.TurnOn();    
                }
                else
                {
                    tt.TurnOff();
                }
            }
        }
        else if (_lastState.HasPose(2))
        {
            _totemsOn = true;
            foreach (var (tt, _) in _totems)
            {
                if (tt.Type == "nat")
                {
                    tt.TurnOn();
                }
                else
                {
                    tt.TurnOff();
                }
            }
        }
        else if (_totemsOn)
        {
            _totemsOn = false;
            foreach (var (tt, _) in _totems)
            {
                tt.TurnOff();
                tt.ShuffleType();
            }
        }

        if (_lastState.Hit(_lastHitTime))
        {
            _lastHitTime = _lastState.last_hit.time;
            CreateBird();
        }

        if (_visibleScore != _score)
        {
            _visibleScore += Math.Sign(_score - _visibleScore);
            scoreText.text = _visibleScore.ToString().PadLeft(3, '0');
        }

        if (_visibleScore >= MaxScore && _score == _visibleScore && !_backwards)
        {
            winText.gameObject.SetActive(true);
            TurnBackwards(true);
            _score = 0;
        }
    }

    private void TurnBackwards(bool b)
    {
        _backwards = b;
        _generateNumber = 3;
    }

    private void SetGenNumberText()
    {
        var symbol = _generateNumber >= 0 ? ">" : "<";
        var text = symbol + symbol + symbol;
        switch (_generateNumber)
        {
            case 1:
                text = symbol;
                break;
            case 2:
            case 3:
                text = symbol + symbol;
                break;
        }

        genNumberText.text = text;
    }

    private void HideWindows()
    {
        winText.gameObject.SetActive(false);
        looseText.gameObject.SetActive(false);
        dangerText.gameObject.SetActive(false);
    }

    public void Start()
    {
        HideWindows();
        
        InitAsIcosohedron();
        Subdivide(3);
        CalculateNeighbors();

        Color32 colorOcean     = new Color32(  178,  0, 183,   0);
        Color32 colorGrass     = new Color32(  249, 129,   0,   0);
        Color32 colorDirt      = new Color32(219, 104,  76,   0);
        Color32 colorDeepOcean = new Color32(  119,  5, 120,   0);

        foreach (Polygon p in m_Polygons)
            p.m_Color = colorOcean;

        PolySet landPolys = new PolySet();
        PolySet sides;

        // Grab polygons that are inside random spheres. These will be the basis of our planet's continents.

        for(int i = 0; i < m_NumberOfContinents; i++)
        {
            float continentSize = Random.Range(m_ContinentSizeMin, m_ContinentSizeMax);
            PolySet newLand = GetPolysInSphere(Random.onUnitSphere, continentSize, m_Polygons);
            landPolys.UnionWith(newLand);
        }
        
        foreach (Polygon landPoly in landPolys)
        {
            landPoly.m_Color = colorGrass;
        }
        
        var oceanPolys = new PolySet();
        foreach (Polygon poly in m_Polygons)
        {
            if (!landPolys.Contains(poly))
                oceanPolys.Add(poly);
        }
        
        var oceanSurface = new PolySet(oceanPolys);
        sides = Inset(oceanSurface, 0.05f);
        //sides.ApplyColor(colorGrass);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        if (m_OceanMesh != null)
            Destroy(m_OceanMesh);

        m_OceanMesh = GenerateMesh("Ocean Surface", m_OceanMaterial);
        
        sides = Extrude(landPolys, ExtrudeHeight);
        sides.ApplyColor(colorDirt);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);
        
        PolySet hillPolys = landPolys.RemoveEdges();

        sides = Inset(hillPolys, 0.03f);
        sides.ApplyColor(colorGrass);
        sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        sides = Extrude(hillPolys, ExtrudeHeight);
        sides.ApplyColor(colorDirt);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);
        
        // oceans
        sides = Extrude(oceanPolys, -ExtrudeHeight / 2);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        sides = Inset(oceanPolys, 0.02f);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        var deepOceanPolys = oceanPolys.RemoveEdges();

        sides = Extrude(deepOceanPolys, -ExtrudeHeight);
        sides.ApplyColor(colorDeepOcean);

        deepOceanPolys.ApplyColor(colorDeepOcean);
        
        if (m_GroundMesh != null)
            Destroy(m_GroundMesh);

        m_GroundMesh = GenerateMesh("Ground Mesh", m_GroundMaterial);

        AddLand(landPolys);
        AddClouds(oceanPolys);
        AddTotems(m_Polygons);
    }

    private void AddTotems(List<Polygon> mPolygons)
    {
        var points = PointsOnSphere(TotemsCount).ToList();
        while (_totems.Count < TotemsCount)
        {
            var index = Random.Range(0, points.Count);
            var p = points[index];
            points.RemoveAt(index);
            
            var point = Quaternion.AngleAxis(totalRotation, new Vector3(0, 1, 1)) * p;
            var t = Instantiate(totem, point, Quaternion.identity, transform); 
            t.transform.rotation = Quaternion.LookRotation(t.transform.position);
            t.ShuffleType();

            _totems.Add((t, p));
        }

        while (_totems.Count(x => x.Item1.IsDisabled()) < DisabledTotemsCount)
        {
            var tt = _totems[Random.Range(0, _totems.Count)].Item1;
            tt.DisableType();
        }
    }

    private void AddLand(PolySet landPolys)
    {
        foreach (var poly in landPolys)
        {
            if (Random.Range(0, 100) < StartNaturePercent)
            {
                AddNature(poly);
            }
            else
            {
                AddCiv(poly);
            }
        }
    }

    private void AddNature(Polygon poly)
    {
        var obj = landObjects[Random.Range(0, 3)];
        var point = Quaternion.AngleAxis(totalRotation, new Vector3(0, 1, 1)) * poly.Center(m_Vertices);
        
        var t = Instantiate(obj, point, Quaternion.identity, transform);
        t.transform.rotation = Quaternion.LookRotation(t.transform.position);
        t.transform.RotateAround(t.transform.position, t.transform.position, Random.Range(0, 100f));
        UpdatePlanted(poly, t, false);
    }

    private void UpdatePlanted(Polygon poly, GameObject o, bool b)
    {
        if (!_planted.ContainsKey(poly))
        {
            _planted.Add(poly, (o, b));
        }
        else
        {
            Destroy(_planted[poly].Item1);
            _planted[poly] = (o, b);
        }
    }

    private void AddCiv(Polygon poly)
    {
        var obj = landObjects[Random.Range(3, 6)];
        var point = Quaternion.AngleAxis(totalRotation, new Vector3(0, 1, 1)) * poly.Center(m_Vertices);
        
        var t = Instantiate(obj, point, Quaternion.identity, transform);
        t.transform.rotation = Quaternion.LookRotation(t.transform.position);
        t.transform.RotateAround(t.transform.position, t.transform.position, Random.Range(0, 100f));
        UpdatePlanted(poly, t, true);
    }

    public void InitAsIcosohedron()
    {
        m_Polygons = new List<Polygon>();
        m_Vertices = new List<Vector3>();

        // An icosahedron

        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        m_Vertices.Add(new Vector3(-1,  t,  0).normalized);
        m_Vertices.Add(new Vector3( 1,  t,  0).normalized);
        m_Vertices.Add(new Vector3(-1, -t,  0).normalized);
        m_Vertices.Add(new Vector3( 1, -t,  0).normalized);
        m_Vertices.Add(new Vector3( 0, -1,  t).normalized);
        m_Vertices.Add(new Vector3( 0,  1,  t).normalized);
        m_Vertices.Add(new Vector3( 0, -1, -t).normalized);
        m_Vertices.Add(new Vector3( 0,  1, -t).normalized);
        m_Vertices.Add(new Vector3( t,  0, -1).normalized);
        m_Vertices.Add(new Vector3( t,  0,  1).normalized);
        m_Vertices.Add(new Vector3(-t,  0, -1).normalized);
        m_Vertices.Add(new Vector3(-t,  0,  1).normalized);

        // And here's the formula for the 20 sides,
        // referencing the 12 vertices we just created.

        m_Polygons.Add(new Polygon( 0, 11,  5));
        m_Polygons.Add(new Polygon( 0,  5,  1));
        m_Polygons.Add(new Polygon( 0,  1,  7));
        m_Polygons.Add(new Polygon( 0,  7, 10));
        m_Polygons.Add(new Polygon( 0, 10, 11));
        m_Polygons.Add(new Polygon( 1,  5,  9));
        m_Polygons.Add(new Polygon( 5, 11,  4));
        m_Polygons.Add(new Polygon(11, 10,  2));
        m_Polygons.Add(new Polygon(10,  7,  6));
        m_Polygons.Add(new Polygon( 7,  1,  8));
        m_Polygons.Add(new Polygon( 3,  9,  4));
        m_Polygons.Add(new Polygon( 3,  4,  2));
        m_Polygons.Add(new Polygon( 3,  2,  6));
        m_Polygons.Add(new Polygon( 3,  6,  8));
        m_Polygons.Add(new Polygon( 3,  8,  9));
        m_Polygons.Add(new Polygon( 4,  9,  5));
        m_Polygons.Add(new Polygon( 2,  4, 11));
        m_Polygons.Add(new Polygon( 6,  2, 10));
        m_Polygons.Add(new Polygon( 8,  6,  7));
        m_Polygons.Add(new Polygon( 9,  8,  1));
    }

    public void Subdivide(int recursions)
    {
        var midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < recursions; i++)
        {
            var newPolys = new List<Polygon>();
            foreach (var poly in m_Polygons)
            {
                int a = poly.m_Vertices[0];
                int b = poly.m_Vertices[1];
                int c = poly.m_Vertices[2];

                int ab = GetMidPointIndex(midPointCache, a, b);
                int bc = GetMidPointIndex(midPointCache, b, c);
                int ca = GetMidPointIndex(midPointCache, c, a);

                newPolys.Add(new Polygon(a, ab, ca));
                newPolys.Add(new Polygon(b, bc, ab));
                newPolys.Add(new Polygon(c, ca, bc));
                newPolys.Add(new Polygon(ab, bc, ca));
            }

            m_Polygons = newPolys;
        }
    }

    public int GetMidPointIndex(Dictionary<int, int> cache, int indexA, int indexB)
    {
        int smallerIndex = Mathf.Min(indexA, indexB);
        int greaterIndex = Mathf.Max(indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;

        int ret;
        if (cache.TryGetValue(key, out ret))
            return ret;
        
        Vector3 p1 = m_Vertices[indexA];
        Vector3 p2 = m_Vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = m_Vertices.Count;
        m_Vertices.Add(middle);

        cache.Add(key, ret);
        return ret;
    }

    public void CalculateNeighbors()
    {
        foreach (Polygon poly in m_Polygons)
        {
            foreach (Polygon other_poly in m_Polygons)
            {
                if (poly == other_poly)
                    continue;

                if (poly.IsNeighborOf(other_poly))
                    poly.m_Neighbors.Add(other_poly);
            }
        }
    }

    public List<int> CloneVertices(List<int> old_verts)
    {
        List<int> new_verts = new List<int>();
        foreach (int old_vert in old_verts)
        {
            Vector3 cloned_vert = m_Vertices[old_vert];
            new_verts.Add(m_Vertices.Count);
            m_Vertices.Add(cloned_vert);
        }
        return new_verts;
    }

    public PolySet StitchPolys(PolySet polys, out EdgeSet stitchedEdge)
    {
        PolySet stichedPolys = new PolySet();

        stichedPolys.m_StitchedVertexThreshold = m_Vertices.Count;

        stitchedEdge      = polys.CreateEdgeSet();
        var originalVerts = stitchedEdge.GetUniqueVertices();
        var newVerts      = CloneVertices(originalVerts);

        stitchedEdge.Split(originalVerts, newVerts);

        foreach (Edge edge in stitchedEdge)
        {
            // Create new polys along the stitched edge. These
            // will connect the original poly to its former
            // neighbor.

            var stitch_poly1 = new Polygon(edge.m_OuterVerts[0],
                                           edge.m_OuterVerts[1],
                                           edge.m_InnerVerts[0]);
            var stitch_poly2 = new Polygon(edge.m_OuterVerts[1],
                                           edge.m_InnerVerts[1],
                                           edge.m_InnerVerts[0]);
            // Add the new stitched faces as neighbors to
            // the original Polys.
            edge.m_InnerPoly.ReplaceNeighbor(edge.m_OuterPoly, stitch_poly2);
            edge.m_OuterPoly.ReplaceNeighbor(edge.m_InnerPoly, stitch_poly1);

            m_Polygons.Add(stitch_poly1);
            m_Polygons.Add(stitch_poly2);

            stichedPolys.Add(stitch_poly1);
            stichedPolys.Add(stitch_poly2);
        }

        //Swap to the new vertices on the inner polys.
        foreach (Polygon poly in polys)
        {
            for (int i = 0; i < 3; i++)
            {
                int vert_id = poly.m_Vertices[i];
                if (!originalVerts.Contains(vert_id))
                    continue;
                int vert_index = originalVerts.IndexOf(vert_id);
                poly.m_Vertices[i] = newVerts[vert_index];
            }
        }

        return stichedPolys;
    }

    public PolySet Extrude(PolySet polys, float height)
    {
        EdgeSet stitchedEdge;
        PolySet stitchedPolys = StitchPolys(polys, out stitchedEdge);
        List<int> verts = polys.GetUniqueVertices();

        foreach (int vert in verts)
        {
            Vector3 v = m_Vertices[vert];
            v = v.normalized * (v.magnitude + height);
            m_Vertices[vert] = v;
        }

        return stitchedPolys;
    }

    public PolySet Inset(PolySet polys, float insetDistance)
    {
        EdgeSet stitchedEdge;
        PolySet stitchedPolys = StitchPolys(polys, out stitchedEdge);

        Dictionary<int, Vector3> inwardDirections = stitchedEdge.GetInwardDirections(m_Vertices);

        // Push each vertex inwards, then correct
        // it's height so that it's as far from the center of
        // the planet as it was before.

        foreach (KeyValuePair<int, Vector3> kvp in inwardDirections)
        {
            int     vertIndex       = kvp.Key;
            Vector3 inwardDirection = kvp.Value;

            Vector3 vertex = m_Vertices[vertIndex];
            float originalHeight = vertex.magnitude;

            vertex += inwardDirection * insetDistance;
            vertex  = vertex.normalized * originalHeight;
            m_Vertices[vertIndex] = vertex;
        }

        return stitchedPolys;
    }

    public PolySet GetPolysInSphere(Vector3 center, float radius, IEnumerable<Polygon> source)
    {
        PolySet newSet = new PolySet();

        foreach(Polygon p in source)
        {
            foreach(int vertexIndex in p.m_Vertices)
            {
                float distanceToSphere = Vector3.Distance(center, m_Vertices[vertexIndex]);

                if (distanceToSphere <= radius)
                {
                    newSet.Add(p);
                    break;
                }
            }
        }

        return newSet;
    }

    public GameObject GenerateMesh(string name, Material material)
    {
        GameObject meshObject       = new GameObject(name);
        meshObject.transform.parent = transform;

        MeshRenderer surfaceRenderer = meshObject.AddComponent<MeshRenderer>();
        surfaceRenderer.material     = material;

        Mesh terrainMesh = new Mesh();

        int vertexCount = m_Polygons.Count * 3;

        int[] indices = new int[vertexCount];

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals  = new Vector3[vertexCount];
        Color32[] colors   = new Color32[vertexCount];
        Vector2[] uvs      = new Vector2[vertexCount];

        for (int i = 0; i < m_Polygons.Count; i++)
        {
            var poly = m_Polygons[i];

            indices[i * 3 + 0] = i * 3 + 0;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;

            vertices[i * 3 + 0] = m_Vertices[poly.m_Vertices[0]];
            vertices[i * 3 + 1] = m_Vertices[poly.m_Vertices[1]];
            vertices[i * 3 + 2] = m_Vertices[poly.m_Vertices[2]];

            uvs[i * 3 + 0] = poly.m_UVs[0];
            uvs[i * 3 + 1] = poly.m_UVs[1];
            uvs[i * 3 + 2] = poly.m_UVs[2];

            colors[i * 3 + 0] = poly.m_Color;
            colors[i * 3 + 1] = poly.m_Color;
            colors[i * 3 + 2] = poly.m_Color;

            if(poly.m_SmoothNormals)
            {
                normals[i * 3 + 0] = m_Vertices[poly.m_Vertices[0]].normalized;
                normals[i * 3 + 1] = m_Vertices[poly.m_Vertices[1]].normalized;
                normals[i * 3 + 2] = m_Vertices[poly.m_Vertices[2]].normalized;
            }
            else
            {
                Vector3 ab = m_Vertices[poly.m_Vertices[1]] - m_Vertices[poly.m_Vertices[0]];
                Vector3 ac = m_Vertices[poly.m_Vertices[2]] - m_Vertices[poly.m_Vertices[0]];

                Vector3 normal = Vector3.Cross(ab, ac).normalized;

                normals[i * 3 + 0] = normal;
                normals[i * 3 + 1] = normal;
                normals[i * 3 + 2] = normal;
            }
        }

        terrainMesh.vertices = vertices;
        terrainMesh.normals  = normals;
        terrainMesh.colors32 = colors;
        terrainMesh.uv       = uvs;

        terrainMesh.SetTriangles(indices, 0);

        MeshFilter terrainFilter = meshObject.AddComponent<MeshFilter>();
        terrainFilter.mesh = terrainMesh;

        return meshObject;
    }
    
    private void AddClouds(PolySet oceanPolys)
    {
        var i = 0;
        foreach (var poly in oceanPolys)
        {
            if (i++ % 6 == 0)
            {
                var obj = seaObjects[Random.Range(0, seaObjects.Length)];
                var t = Instantiate(obj, poly.Center(m_Vertices) * 1.2f, Quaternion.identity, Clouds);
                t.transform.rotation = Quaternion.LookRotation(t.transform.position);
                t.transform.RotateAround(t.transform.position, t.transform.position, Random.Range(0, 100f));
            }
        }
    }
    
    private Vector3[] PointsOnSphere(int n)
    {
        List<Vector3> upts = new List<Vector3>();
        float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
        float off = 2.0f / n;
        float x = 0;
        float y = 0;
        float z = 0;
        float r = 0;
        float phi = 0;
       
        for (var k = 0; k < n; k++){
            y = k * off - 1 + (off /2);
            r = Mathf.Sqrt(1 - y * y);
            phi = k * inc;
            x = Mathf.Cos(phi) * r;
            z = Mathf.Sin(phi) * r;
           
            upts.Add(new Vector3(x, y, z));
        }
        Vector3[] pts = upts.ToArray();
        return pts;
    }
    
    IEnumerator GetState()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(StateUri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            
            if (webRequest.isNetworkError)
            {
                _requestingNow = false;
                _lastRequest = DateTime.Now + new TimeSpan(0, 0, 1);
            }
            else
            {
                var state = JsonUtility.FromJson<State>(webRequest.downloadHandler.text);
                _lastState = state;
                _requestingNow = false;
            }
        }
    }

    IEnumerator SendMode(int mode, string id)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get($"{ModeUri}/{id}/{mode}"))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
        }
    }
    
    private void CreateBird()
    {
        var startX = leftCam + (rightCam - leftCam) * _lastState.last_hit.x / 800f;
        var startY = topCam - (topCam - bottomCam) * _lastState.last_hit.y / 600f;
        var startPoint = new Vector3(startX, startY, zCam);
        var brd = Instantiate(bird, startPoint, Quaternion.identity);
        brd.Fire(
            startPoint,
            LowerSpeed + (HigherSpeed - LowerSpeed) * _lastState.last_hit.strength,
            HigherAngularSpeed - (HigherAngularSpeed - LowerAngularSpeed) * _lastState.last_hit.strength,
            _lastState.last_hit.id,
            this
        );
        StartCoroutine(SendMode(0, _lastState.last_hit.id));
    }

    public void HitSomething(Vector3 point, string id)
    {
        var minDist = float.MaxValue;
        Totem bestTotem = null;
        foreach (var (tt, _) in _totems)
        {
            var dist = (point - totem.transform.position).magnitude;
            if (minDist < dist)
            {
                minDist = dist;
                bestTotem = tt;
            }
        }

        if (bestTotem != null && minDist < 0.1f)
        {
            if (_lastHitType == "" && !bestTotem.IsDisabled())
            {
                // first hit non neutral
                _lastHitType = bestTotem.Type;
                StartCoroutine(SendMode(_lastHitType == "civ" ? 1 : 2, id));
                _score += 20;
                bestTotem.DisableType();
                bestTotem.HitAnim();
            }
            else if (_lastHitType == bestTotem.Type)
            {
                // second hit right
                _lastHitType = "";
                StartCoroutine(SendMode(0, id));
                bestTotem.FullHitAnim();
                _score += 100;
                _generateNumber += (bestTotem.Type == "civ" ? -2 : 2);
                if (_generateNumber == 0)
                {
                    _generateNumber = 1;
                }
                _lastMutation = DateTime.Now + mutationPeriod;
                ShuffleTotems();
            } 
            else if (_lastHitType != "")
            {
                // second hit wrong
                _generateNumber += Math.Sign(_generateNumber);
                if (_generateNumber == 0)
                {
                    _lastMutation = DateTime.MinValue;
                }
                bestTotem.DisableType();
            }
            else
            {
                // first hit neutral
                bestTotem.HitAnim();
            }

            SetGenNumberText();
        }
        else
        {
            HitNone(point, id);
        }
    }

    private void ShuffleTotems()
    {
        foreach (var (tt, _) in _totems)
        {
            tt.ShuffleType();
            tt.TurnOff();
        }
        
        while (_totems.Count(x => x.Item1.IsDisabled()) < DisabledTotemsCount)
        {
            var tt = _totems[Random.Range(0, _totems.Count)].Item1;
            tt.DisableType();
        }
    }

    public void HitNone(Vector3 transformPosition, string id)
    {
        var h = Instantiate(hitNone, transformPosition, Quaternion.identity);
        h.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
    }
}
