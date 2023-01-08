using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.IO.Enumeration;
using UnityEngine;
using Pngcs.Unity;
using Newtonsoft.Json;
using Object = UnityEngine.Object;


public class DepthCamera
{
    public Camera Cam;
    public Shader DepthShader;
    public Shader NormalShader;

    public DepthCamera(Camera camera, Shader depthShader, Shader normalShader)
    {
        Cam = camera;
        DepthShader = depthShader;
        NormalShader = normalShader;
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

    public Calibration GetProjectorMatrix(Light projector)
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


    public static void SaveScene(string filePrefix, List<GameObject> objInstances, DepthCamera depthCamera, List<ObjectStruct> sceneData)
    {
        Calibration camera0 = depthCamera.GetCameraMatrix();

        string depthFileName = filePrefix + ".depth0.png";
        DepthMap depthMap0 = depthCamera.CaptureDepth(depthFileName, objInstances);

        string normalFileName = filePrefix + ".normal0.png";
        depthCamera.CaptureNormal(normalFileName, camera0.R, objInstances);

        string sceneFileName = filePrefix + ".image.png";
        depthCamera.CaptureScene(sceneFileName);

        string dataFileName = filePrefix + ".data0.json";
        depthCamera.writeDataFileOneView(dataFileName, camera0, depthMap0, sceneData);

        // environmentMap.SetTexture("_Tex", danceRoomEnvironment);
        // DestroyObjs(objInstances);
    }
    
    public Calibration GetCameraMatrix()
    {
        Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Inverse(Cam.transform.rotation));
        R[1, 0] = -R[1, 0];
        R[1, 1] = -R[1, 1];
        R[1, 2] = -R[1, 2];
        Vector3 t = R * -Cam.transform.position;
        R[0, 2] = -R[0, 2];
        R[1, 2] = -R[1, 2];
        R[2, 2] = -R[2, 2];
        Matrix4x4 K = Matrix4x4.identity;
        K[0, 0] = Cam.focalLength;
        K[1, 1] = K[0, 0];
        RenderTexture camTargetTexture = Cam.targetTexture;
        K[0, 2] = (float)(camTargetTexture.width + 1) / 2;
        K[1, 2] = (float)(camTargetTexture.height + 1) / 2;

        Calibration calibration = new Calibration(K, R, t);

        return calibration;
    }

    public void writeDataFileOneView(string fileName, Calibration camera, DepthMap depthMap, List<ObjectStruct> sceneData)
    {
        StreamWriter writer = new StreamWriter(fileName, false);
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

        
        String objectData = "\"objects\":[";
        for (int i = 0; i < sceneData.Count; i++)
        {
            objectData += "{" +
                          "\"id\":" + sceneData[i].Id + "," +
                          "\"shape\":\"" + sceneData[i].Shape + "\"" + "," +
                          "\"size\":" + sceneData[i].Size  + "," +
                          "\"material\":\"" + sceneData[i].Material + "\"" + "," +
                          "\"position\":[" +
                          (float)sceneData[i].Position[0] + "," +
                          (float)sceneData[i].Position[1] + "," +
                          (float)sceneData[i].Position[2] +
                          "]" +
                          "}";
            if (i != sceneData.Count - 1)
            {
                objectData += ",";
            }
        }

        objectData += "]";

        writer.Write(data1 + objectData + "}");
        writer.Close();
    }
    
    
    public void CaptureScene(string fileName)
    {
        RenderTexture.active = Cam.targetTexture;
        // projector.cookie = white;

        Cam.Render();
        Texture2D currentTexture =
            new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height, TextureFormat.RGB24, false);
        currentTexture.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
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

        PNG.Write(imageRGB, w, h, 8, false, false, fileName);
        //PNG.Write(imageRGB, w, h, 8, false, true, saved_path + fileCounter.ToString("D5") + "." + name + "RGB.png");
        // Destroy(currentTexture);
    }
    
    
    public void CaptureNormal(string fileName, Matrix4x4 R, List<GameObject> objs)
    {
        RenderTexture.active = Cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        Cam.RenderWithShader(NormalShader, "RenderType");
        Texture2D image = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(false);
        }

        Cam.RenderWithShader(NormalShader, "RenderType");
        Texture2D cubeImage = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        cubeImage.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        cubeImage.Apply();
        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(true);
        }

        // environmentMap.SetTexture("_Tex", env);
        Color[] normalMap = new Color[Cam.targetTexture.width * Cam.targetTexture.height];

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

        PNG.Write(normalMap, w, h, 8, false, false,
            fileName);

        // Destroy(image);
        // Destroy(cubeImage);
    }
    
    public DepthMap CaptureDepth(string fileName, List<GameObject> objs)
    {
        RenderTexture.active = Cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        Cam.RenderWithShader(DepthShader, "RenderType");
        Texture2D image = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(false);
        }

        Cam.RenderWithShader(DepthShader, "RenderType");
        Texture2D spinCubeImage = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height,
            TextureFormat.RGBAFloat, false);
        spinCubeImage.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        spinCubeImage.Apply();

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(true);
        }

        DepthMap depthMap = new DepthMap(Cam.targetTexture.width, Cam.targetTexture.height);

        int w = image.width;
        int h = image.height;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                UnityEngine.Color c = image.GetPixel(j, i);
                UnityEngine.Color s = spinCubeImage.GetPixel(j, i);
                float dz = (float)(1 - c.r) * (float)((float)Cam.farClipPlane);
                float dx = (float)((float)(w + 1) / 2 - (float)j - (float)1) / (float)Cam.focalLength * dz;
                float dy = (float)((float)(h + 1) / 2 - (float)i - (float)1) / (float)Cam.focalLength * dz;
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

        PNG.Write(depthMap.depths, w, h, 16, false, true, fileName);
        
 
        // Destroy(image);
        // Destroy(spinCubeImage);

        return depthMap;
    }
}