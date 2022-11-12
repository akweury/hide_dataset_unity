using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pngcs.Unity;

public class VisualObjs : MonoBehaviour
{
    static int OBJ_NUM = 3;

    int MAX_NUM_TRIES = 50;
    float TABLE_WIDTH_BASE = 1;
    float TABLE_LENGTH_BASE = 1;
    float TABLE_HEIGHT_BASE = (float)0.1;
    float UNIFY_RADIUS = (float)0.5;
    float MINIMUM_OBJ_DIST = (float)0.001;


    public class DepthMap
    {
        public Color[] depths;
        public int width;
        public int height;
        public float minDepth;
        public float maxDepth;

        public DepthMap(int w, int h)
        {
            depths = new Color[w * h];
            width = w;
            height = h;
            minDepth = (float)Mathf.Infinity;
            maxDepth = (float)0.0;
        }
    }

    public class Calibration
    {
        public Matrix4x4 K;
        public Matrix4x4 R;
        public Vector3 t;

        public Calibration(Matrix4x4 KK, Matrix4x4 RR, Vector3 tt)
        {
            K = KK;
            R = RR;
            t = tt;
        }
    }

    public struct Point3D
    {
        public float x;
        public float y;
        public float z;
    }

    public struct Obj3D
    {
        public Point3D p;
        public float radius;
    }

    public int single_idx = 1;
    public List<GameObject> train_models;
    public List<GameObject> test_models;
    public List<Material> materials;
    public GameObject table;
    public Material environmentMap;
    public Cubemap blackEnvironment;
    public List<Cubemap> environments;

    public RenderTexture texture;

    // public Light dirLight;
    public Light projector;

    // public Texture2D phaseH;
    // public Texture2D phaseV;
    // public Texture2D white;
    public Shader depthShader;

    // public Shader phaseShader;
    public Shader normalShader;


    private Camera cam;
    List<GameObject> modelInsts = new List<GameObject>(new GameObject[OBJ_NUM]);
    GameObject tableInst;

    private int frames = 0;
    int lightSceneNum = 1;
    float table_length;
    float table_width;
    float table_height;
    int fileCounter = 0;
    int maxFileCounter = 3000;
    int train_end_idx = 2500;
    int validation_end_idx = 2750;
    int test_end_idx = 3000;
    int modelIdx = -1;
    float scale_factor = 1;

    string file_type;
    bool newScene;
    string saved_path;
    string root_path;

    List<GameObject> models;


    // Start is called before the first frame update
    void Start()
    {
        cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        cam.targetTexture = texture;


        RenderSettings.ambientLight = UnityEngine.Color.gray;
        // table.GetComponent<GameObject>().enabled = true;
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/train");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/val");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/test");

        root_path = Application.dataPath + "/../CapturedData/data_synthetic/";
        // file_type = "synthetic";
        models = train_models;

        // adjust table size based on camera sensor size
        scale_factor = (float)(((cam.sensorSize[0] + cam.sensorSize[1]) / 2) / 128);
        table_length = TABLE_LENGTH_BASE * scale_factor;
        table_width = TABLE_WIDTH_BASE * scale_factor;
        table_height = TABLE_HEIGHT_BASE * scale_factor;
    }

    // Update is called once per frame
    void Update()
    {
        // change train/eval/test models
        if (fileCounter >= validation_end_idx)
        {
            saved_path = root_path + "test/";
            models = test_models;
        }
        else if (fileCounter >= train_end_idx)
        {
            models = test_models;
            saved_path = root_path + "val/";
        }
        else if (fileCounter >= 0)
        {
            saved_path = root_path + "train/";
        }

        // save the scene data if it is a new scene
        if (newScene == true)
        {
            Calibration camera0 = getCameraMatrix();
            DepthMap depthMap0 = CaptureDepth("depth0");
            CaptureNormal("normal0", camera0.R);
            // Calibration[] projectors = new Calibration[10];
            // for (int i = 0; i < lightSceneNum; i++)
            // {
            //     projectors[i] = getProjectorMatrix();
            //     CaptureScene("image" + i.ToString());
            // }

            CaptureScene("image");
            // writeDataFileOneView("data0", modelInsts.name, camera0, depthMap0, projectors);
            // environmentMap.SetTexture("_Tex", blackEnvironment);
            newScene = false;
            fileCounter++;
        }

        // render the scene


        if (frames % 5 == 0 && fileCounter < maxFileCounter)
        {
            for (int i = 0; i < OBJ_NUM; i++)
            {
                if (modelInsts[i] != null) Destroy(modelInsts[i]);
            }

            if (tableInst != null) Destroy(tableInst);

            int envIdx = new System.Random().Next(0, environments.Count);
            if (frames % 5 == 0) modelIdx = (modelIdx + 1) % models.Count;

            // instantiate the table
            Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
                UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

            Vector3 scale = new Vector3(table_length, table_height, table_width);
            table.transform.localScale = scale;


            // instantiate the objects on the table
            modelInsts = addRandomObjs();

            newScene = true;
        }

        frames++;

        // stop rendering when file number exceeded the threshold
        if (fileCounter >= test_end_idx)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }

    List<GameObject> addRandomObjs()
    {
        List<GameObject> objInsts = new List<GameObject>(new GameObject[OBJ_NUM]);

        List<Obj3D> obj3Ds = new List<Obj3D>();
        for (int i = 0; i < OBJ_NUM; i++)
        {
            int num_tries = 0;
            float new_scale;
            Obj3D new_obj_3D;
            while (true)
            {
                num_tries += 1;
                // exceed the maximum trying time
                if (num_tries > MAX_NUM_TRIES) return addRandomObjs();
                // choose new size and position
                new_scale = UnityEngine.Random.Range((float)0.1, (float)0.3) * scale_factor;

                new_obj_3D.p.x = 1.5f + UnityEngine.Random.Range(-(float)(table_width / 2), (float)(table_width / 2));
                new_obj_3D.p.y = -4f + table_height * 0.5f + 0.5f * new_scale;
                new_obj_3D.p.z = 18f + UnityEngine.Random.Range(-(float)(table_length / 2), (float)(table_length / 2));
                new_obj_3D.radius = new_scale * UNIFY_RADIUS;

                // check for overlapping
                bool dists_good = true;
                // bool margins_good = true;
                for (int j = 0; j < obj3Ds.Count; j++)
                {
                    float dx = new_obj_3D.p.x - obj3Ds[j].p.x;
                    float dz = new_obj_3D.p.z - obj3Ds[j].p.z;
                    float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (dist - new_obj_3D.radius - obj3Ds[j].radius < MINIMUM_OBJ_DIST)
                    {
                        dists_good = false;
                        break;
                    }
                }

                if (dists_good) break;
            }

            // choose random material and shape
            int objIdx = UnityEngine.Random.Range(0, models.Count);
            objInsts[i] = NewObjectInstantiate(new_scale, new_obj_3D.p, objIdx);
            objInsts[i].GetComponent<Renderer>().material = materials[i];

            // for cubes, adjust its radius
            if (objInsts[i].name == "Cube") new_obj_3D.radius = new_obj_3D.radius / (float)Math.Sqrt(2);


            // record the data about the object in the scene data structure
            obj3Ds.Add(new_obj_3D);
        }

        return objInsts;
    }

    GameObject NewObjectInstantiate(float scale, Point3D newPoint, int objIdx)
    {
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        // random place the object on the table
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(models[objIdx], position, rotation);
        // GameObject objInst = Instantiate(models[objIdx], new Vector3(1.5f, -4f, 18f), rotation);
        objInst.name = models[objIdx].name;
        objInst.transform.localScale *= scale;

        return objInst;
    }

    void CaptureScene(string name)
    {
        RenderTexture.active = cam.targetTexture;
        // projector.cookie = white;

        cam.Render();
        Texture2D currentTexture =
            new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGB24, false);
        currentTexture.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        currentTexture.Apply();

        int w = currentTexture.width;
        int h = currentTexture.height;
        Color[] imageGray = new Color[w * h];
        Color[] imageRGB = new Color[w * h];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                UnityEngine.Color c = currentTexture.GetPixel(j, i);
                float gray_val = (c.r + 2 * c.g + c.b) / 4;
                imageGray[i * w + j] = new UnityEngine.Color(gray_val, 0, 0, 0);
                imageRGB[i * w + j] = new UnityEngine.Color(c.r, c.g, c.b, 0);
            }
        }

        PNG.Write(imageRGB, w, h, 8, false, false, saved_path + fileCounter.ToString("D5") + "." + name + ".png");
        //PNG.Write(imageRGB, w, h, 8, false, true, saved_path + fileCounter.ToString("D5") + "." + name + "RGB.png");
        Destroy(currentTexture);
    }


    DepthMap CaptureDepth(string name)
    {
        RenderTexture.active = cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        cam.RenderWithShader(depthShader, "RenderType");
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < OBJ_NUM; i++)
        {
            modelInsts[i].SetActive(false);
        }


        cam.RenderWithShader(depthShader, "RenderType");
        Texture2D spinCubeImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height,
            TextureFormat.RGBAFloat, false);
        spinCubeImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        spinCubeImage.Apply();

        for (int i = 0; i < OBJ_NUM; i++)
        {
            modelInsts[i].SetActive(true);
        }

        // environmentMap.SetTexture("_Tex", env);

        DepthMap depthMap = new DepthMap(cam.targetTexture.width, cam.targetTexture.height);

        int w = image.width;
        int h = image.height;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                UnityEngine.Color c = image.GetPixel(j, i);
                UnityEngine.Color s = spinCubeImage.GetPixel(j, i);
                float dz = (float)(1 - c.r) * (float)((float)cam.farClipPlane);
                float dx = (float)((float)(w + 1) / 2 - (float)j - (float)1) / (float)cam.focalLength * dz;
                float dy = (float)((float)(h + 1) / 2 - (float)i - (float)1) / (float)cam.focalLength * dz;
                float depth = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                if (c.r == 0)
                    depth = 0;
                if (s.r != 0 && s.r == c.r)
                    depth = 0; // filter out spin cube
                depthMap.depths[i * w + j] = new UnityEngine.Color(depth, 0, 0, 0);
                if (depth != 0)
                {
                    if (depth > depthMap.maxDepth)
                        depthMap.maxDepth = depth;
                    if (depth < depthMap.minDepth)
                        depthMap.minDepth = depth;
                }
            }
        }

        for (int i = 0; i < h * w; i++)
        {
            if (depthMap.depths[i].r != 0)
                depthMap.depths[i].r =
                    (depthMap.depths[i].r - depthMap.minDepth) /
                    (depthMap.maxDepth - depthMap.minDepth); // normalise all the R-channel values in range [0,1]
        }

        PNG.Write(depthMap.depths, w, h, 16, false, true,
            saved_path + fileCounter.ToString("D5") + "." + name + ".png");

        Destroy(image);
        Destroy(spinCubeImage);

        return depthMap;
    }

    void CaptureNormal(string name, Matrix4x4 R)
    {
        RenderTexture.active = cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        cam.RenderWithShader(normalShader, "RenderType");
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < OBJ_NUM; i++)
        {
            modelInsts[i].SetActive(false);
        }

        cam.RenderWithShader(normalShader, "RenderType");
        Texture2D cubeImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        cubeImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        cubeImage.Apply();
        for (int i = 0; i < OBJ_NUM; i++)
        {
            modelInsts[i].SetActive(true);
        }

        // environmentMap.SetTexture("_Tex", env);
        Color[] normalMap = new Color[cam.targetTexture.width * cam.targetTexture.height];

        int w = image.width;
        int h = image.height;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                UnityEngine.Color c = image.GetPixel(j, i);
                UnityEngine.Color s = cubeImage.GetPixel(j, i);
                // filter out the cube part
                if (s.a != 0 && s.r == c.r && s.g == c.g && s.b == c.b)
                {
                    c.a = 0;
                    c.r = 0;
                    c.g = 0;
                    c.b = 0;
                }

                if (c.a != 0)
                    c.g = 1 - c.g;

                normalMap[i * w + j] = c;
            }
        }

        PNG.Write(normalMap, w, h, 8, false, false, saved_path + fileCounter.ToString("D5") + "." + name + ".png");

        Destroy(image);
        Destroy(cubeImage);
    }

    void writeDataFileOneView(string name, string model_name, Calibration camera, DepthMap depthMap,
        Calibration[] projectors)
    {
        StreamWriter writer = new StreamWriter(saved_path + fileCounter.ToString("D5") + "." + name + ".json", false);


        String data1 = "{" + "\"K\":[[" +
                       camera.K[0, 0] + "," + 0 + "," +
                       camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," +
                       camera.K[1, 1] + "," +
                       camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," + 0 + "," + 1 + "]]" + "," +
                       "\"R\":[[" +
                       camera.R[0, 0].ToString().Replace(',', '.') + "," +
                       camera.R[0, 1].ToString().Replace(',', '.') + "," +
                       camera.R[0, 2].ToString().Replace(',', '.') + "],[" +
                       camera.R[1, 0].ToString().Replace(',', '.') + "," +
                       camera.R[1, 1].ToString().Replace(',', '.') + "," +
                       camera.R[1, 2].ToString().Replace(',', '.') + "],[" +
                       camera.R[2, 0].ToString().Replace(',', '.') + "," +
                       camera.R[2, 1].ToString().Replace(',', '.') + "," +
                       camera.R[2, 2].ToString().Replace(',', '.') + "]]" + "," + "\"t\":[" +
                       camera.t[0].ToString().Replace(',', '.') + "," +
                       camera.t[1].ToString().Replace(',', '.') + "," +
                       camera.t[2].ToString().Replace(',', '.') + "]" + "," + "\"minDepth\":" +
                       depthMap.minDepth.ToString().Replace(',', '.') + "," + "\"maxDepth\":" +
                       depthMap.maxDepth.ToString().Replace(',', '.') + ",";


        String data2 = "";
        for (int i = 0; i < lightSceneNum; i++)
        {
            // Vector3 projectorPos = projectors[i].R.transpose * -projectors[i].t;
            Vector3 projectorPos = projectors[i].t;

            data2 += "\"lightPos" + i + "\":[" + projectorPos[0].ToString().Replace(',', '.') + "," +
                     projectorPos[1].ToString().Replace(',', '.') + "," +
                     projectorPos[2].ToString().Replace(',', '.') + "]" + ",";
        }


        String data3 = "\"name\":\"" + model_name + "\"" + "}";

        writer.Write(data1 + data2 + data3);
        writer.Close();

        //writer.Write("{" + "\"K\":[[" +
        //    camera.K[0, 0] + "," + 0 + "," +
        //    camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," +
        //    camera.K[1, 1] + "," +
        //    camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," + 0 + "," + 1 + "]]" + "," + "\"R\":[[" +
        //    camera.R[0, 0].ToString().Replace(',', '.') + "," +
        //    camera.R[0, 1].ToString().Replace(',', '.') + "," +
        //    camera.R[0, 2].ToString().Replace(',', '.') + "],[" +
        //    camera.R[1, 0].ToString().Replace(',', '.') + "," +
        //    camera.R[1, 1].ToString().Replace(',', '.') + "," +
        //    camera.R[1, 2].ToString().Replace(',', '.') + "],[" +
        //    camera.R[2, 0].ToString().Replace(',', '.') + "," +
        //    camera.R[2, 1].ToString().Replace(',', '.') + "," +
        //    camera.R[2, 2].ToString().Replace(',', '.') + "]]" + "," + "\"t\":[" +
        //    camera.t[0].ToString().Replace(',', '.') + "," +
        //    camera.t[1].ToString().Replace(',', '.') + "," +
        //    camera.t[2].ToString().Replace(',', '.') + "]" + "," + "\"minDepth\":" +
        //    depthMap.minDepth.ToString().Replace(',', '.') + "," + "\"maxDepth\":" +
        //    depthMap.maxDepth.ToString().Replace(',', '.') + "," + "\"lightPos\":[" +
        //    projectorPos[0].ToString().Replace(',', '.') + "," +
        //    projectorPos[1].ToString().Replace(',', '.') + "," +
        //    projectorPos[2].ToString().Replace(',', '.') + "]" + "," + "\"name\":\""+
        //    model_name+"\"" + "}");
        //writer.Close();
    }

    Calibration getCameraMatrix()
    {
        Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Inverse(cam.transform.rotation));
        R[1, 0] = -R[1, 0];
        R[1, 1] = -R[1, 1];
        R[1, 2] = -R[1, 2];
        Vector3 t = R * -cam.transform.position;
        R[0, 2] = -R[0, 2];
        R[1, 2] = -R[1, 2];
        R[2, 2] = -R[2, 2];
        Matrix4x4 K = Matrix4x4.identity;
        K[0, 0] = cam.focalLength;
        K[1, 1] = cam.focalLength;
        K[0, 2] = (float)(cam.targetTexture.width + 1) / 2;
        K[1, 2] = (float)(cam.targetTexture.height + 1) / 2;

        Calibration camera = new Calibration(K, R, t);

        return camera;
    }

    Calibration getProjectorMatrix()
    {
        Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Inverse(projector.transform.rotation));
        R[1, 0] = -R[1, 0];
        R[1, 1] = -R[1, 1];
        R[1, 2] = -R[1, 2];
        // Vector3 t = R * -cam.transform.position;
        Vector3 t = projector.transform.position;
        R[0, 2] = -R[0, 2];
        R[1, 2] = -R[1, 2];
        R[2, 2] = -R[2, 2];
        Matrix4x4 K = Matrix4x4.identity;

        Calibration projectorCalibration = new Calibration(K, R, t);

        return projectorCalibration;
    }
}