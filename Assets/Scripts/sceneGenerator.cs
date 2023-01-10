using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.SearchService;
using UnityEngine;
using utils;

public class SceneGenerator : MonoBehaviour
{
    // useful public variables
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public List<GameObject> sphereModels;
    public List<GameObject> cubeModels;
    public RenderTexture texture;
    public Shader depthShader;
    public Shader normalShader;

    // useful private variables
    private string _rootPath;
    private string _datasetPath;

    private string _outputPath;
    private string _inputPath;

    private string _filePrefix;
    private int _frames;

    // private int _ruleFileCounter;
    private int _fileCounter;
    private List<GameObject> _objInstances;
    private List<MeshRenderer> _modelRenderers;
    private List<ObjectStruct> _sceneData;
    private DepthCamera _depthCamera;
    private const float TableWidthBase = 1.5F;
    private const float TableLengthBase = 1.5F;
    private const float TableHeightBase = 0.1F;
    private const float ScaleFactor = 4F;
    private const int FrameLoop = 10;
    private float _tableLength;
    private float _tableWidth;
    private float _tableHeight;
    private FileInfo[] _files;
    private string _sceneType;
    private SceneJson _sceneJson;

    private bool _sceneDone;

    // Start is called before the first frame update
    void Start()
    {
        _rootPath = Application.dataPath + "/../../spatial_relation_vector/storage/";
        _inputPath = _rootPath + "output/02.scene_modification/";
        _outputPath = _rootPath + "output/03.scene_visualization/";


        Camera cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        _depthCamera = new DepthCamera(cam, depthShader, normalShader);
        _depthCamera.Cam.targetTexture = texture;
        RenderSettings.ambientLight = Color.gray;


        // get all json files
        DirectoryInfo d = new DirectoryInfo(_inputPath);
        _files = d.GetFiles("*.json"); // Getting Rule files
        _fileCounter = 0;

        // load a new scene
        _sceneJson = LoadNewScene(_files[_fileCounter].FullName);
        _filePrefix = _outputPath + "Test_output_" + _fileCounter;
        // _fileCounter++;

        // instantiate the table
        // adjust table size based on camera sensor size
        _tableLength = TableLengthBase * ScaleFactor;
        _tableWidth = TableWidthBase * ScaleFactor;
        _tableHeight = TableHeightBase * ScaleFactor;
        table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
    }

    // Update is called once per frame
    void Update()
    {
        // render the scene
        if (_frames % FrameLoop == 0)
        {
            RenderNewScene(_sceneJson, sphereModels, cubeModels, _sceneType);
        }

        // save the scene data if it is a new scene
        if (_frames % FrameLoop == FrameLoop - 1)
        {
            DepthCamera.SaveCompareScene(_inputPath,_outputPath, _fileCounter, _objInstances, _depthCamera, _sceneData);
            DestroyObjs(_objInstances);
            _sceneDone = true;
            _fileCounter++;
            _filePrefix = _outputPath + "Test_output_" + _fileCounter;
        }


        // load a new rule file
        if (_sceneDone)
        {
            DestroyObjs(_objInstances);
            if (_fileCounter >= _files.Length)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                _sceneJson = LoadNewScene(_files[_fileCounter].FullName);
                _sceneDone = false;
                // _fileCounter++;
            }
        }

        // stop rendering when file number exceeded the threshold
        _frames++;
    }

    SceneJson LoadNewScene(string sceneFileName)
    {
        StreamReader streamReader = new StreamReader(sceneFileName);
        string json = streamReader.ReadToEnd();
        SceneJson sceneJson = JsonConvert.DeserializeObject<SceneJson>(json);
        return sceneJson;
    }


    void RenderNewScene(SceneJson sceneJson, List<GameObject> spheres, List<GameObject> cubes, string sceneType)
    {
        int objNum = sceneJson.Objs.Count;
        _sceneData = new List<ObjectStruct>(new ObjectStruct[objNum]);
        _objInstances = new List<GameObject>(new GameObject[objNum]);

        _sceneData = FillSceneData(sceneJson, _sceneData);

        int objId = 0;
        foreach (var sceneObj in _sceneData)
        {
            float objSize = sceneObj.Size;

            // for cubes, adjust its radius
            if (sceneObj.Shape == "cube") objSize = sceneObj.Size / (float)Math.Sqrt(2);

            // match the obj with a GameObject
            GameObject objModel = spheres[0];
            if (sceneObj.Shape == "sphere")
            {
                foreach (GameObject sphere in spheres)
                {
                    if (sphere.name == sceneObj.Material)
                    {
                        objModel = sphere;
                        break;
                    }
                }
            }
            else if (sceneObj.Shape == "cube")
            {
                foreach (GameObject cube in cubes)
                {
                    if (cube.name == sceneObj.Material)
                    {
                        objModel = cube;
                        break;
                    }
                }
            }

            _objInstances[objId] = NewObjectInstantiate(objSize * 2, sceneObj.Position, objModel);
            objId++;
        }
    }

    List<ObjectStruct> FillSceneData(SceneJson sceneJson, List<ObjectStruct> sceneData)
    {
        for (int oldIdx = 0; oldIdx < sceneJson.Objs.Count; oldIdx++)
        {
            // match the predicted object with the original scene 
            int objId = sceneJson.Objs[oldIdx].Id;
            SceneJson.ObjJson objCandidate = sceneJson.SceneConfig.Objects[0];
            SceneJson.ObjJson predObjCandidate = sceneJson.PredObjs[0];

            float minX = 1000;
            float minZ = 1000;
            for (int i = 0; i < sceneJson.SceneConfig.Objects.Count; i++)
            {
                float xDiff = Mathf.Abs(sceneJson.SceneConfig.Objects[i].screenPos[0] -
                                        sceneJson.Objs[oldIdx].screenPos[0]);
                // float zDiff = Mathf.Abs(configObj.screenPos[2] - sceneObj.screenPos[2]);
                if (xDiff < minX)
                {
                    minX = xDiff;
                    objCandidate = sceneJson.SceneConfig.Objects[i];
                }
            }

            predObjCandidate = sceneJson.PredObjs[oldIdx];
            
            float newX = predObjCandidate.Pos[0];
            float oldY = sceneJson.Objs[oldIdx].Pos[1];
            float newZ = predObjCandidate.Pos[2];
            // record the new object
            string shape = objCandidate.Shape;
            string material = objCandidate.Material;
            float size = objCandidate.Size;
            float[] position = predObjCandidate.Pos;
            sceneData[objId] = new ObjectStruct(objId, shape, material, size)
            {
                Position = new Vector3(newX, oldY, newZ)
            };
        }

        return sceneData;
    }

    GameObject NewObjectInstantiate(float scale, Vector3 newPoint, GameObject newModel)
    {
        // place the object on the table
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(newModel, position, rotation);

        objInst.name = newModel.name;
        objInst.transform.localScale = new Vector3(scale, scale, scale);

        return objInst;
    }

    void DestroyObjs(List<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            if (obj) Destroy(obj);
        }
    }
}