using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OscJack;
using UnityEngine;
using UnityEngine.Events;

namespace MDF.YOLO11
{
    public class BodyTracker : MonoBehaviour
    {
        public event UnityAction<BodyKeypoint> ValueChanged;

        [Header("Tracking Options")]
        public List<COCOKeypoint> trackingJoints = new() { COCOKeypoint.Nose, COCOKeypoint.LeftWrist, COCOKeypoint.RightWrist };
        public bool invertX = false;
        public bool invertY = false;

        [Header("OSC Settings")]
        public int port = 5005;
        public string path = "/yolo";

        private OscServer _server;
        private List<JToken> _buffer;
        private BodyKeypoint _keypoints;
        private bool _updated;

        private void Start()
        {
            _server = new OscServer(port);

            // 接收到 OSC 輸入時呼叫 LoadResult
            // LoadResult 會在另一個執行續跑
            _server.MessageDispatcher.AddCallback(path, LoadResult);
        }

        private void OnDisable()
        {
            _server.Dispose();
        }

        private void Update()
        {
            // 在主執行續 Invoke 更新事件
            if (_updated)
            {
                ValueChanged?.Invoke(_keypoints);
                _updated = false;
            }
        }

        /// <summary>
        /// 讀取 OSC 輸入並進行快取
        /// </summary>
        /// <param name="_"></param>
        /// <param name="data"></param>
        private void LoadResult(string _, OscDataHandle data)
        {
            var raw = data.GetElementAsString(0);
            var result = JArray.Parse(raw);
            CacheKeypoints(result);
        }


        /// <summary>
        /// 快取關節點座標
        /// </summary>
        /// <param name="result">快取結果</param>
        private void CacheKeypoints(JArray result)
        {
            _buffer = new List<JToken>(result.Count);
            foreach (var r in result)
            {
                var keypoints = r["keypoints"];
                _buffer.Add(keypoints);
            }

            _keypoints = new BodyKeypoint();
            for (var i = 0; i < _buffer.Count; i++)
            {
                var points = new Dictionary<COCOKeypoint, Vector2>();
                foreach (var j in trackingJoints)
                {
                    points.Add(j, GetKeypointPosition(_buffer, i, j));
                }
                _keypoints.Add(points);
            }

            _updated = true;
        }

        /// <summary>
        /// 取得 XY 座標 (invertX、invertY 座標反轉)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="joint"></param>
        /// <returns></returns>
        private Vector2 GetKeypointPosition(List<JToken> buffer, int index, COCOKeypoint joint)
        {
            var x = (float)buffer[index]["x"][(int)joint];
            var y = (float)buffer[index]["y"][(int)joint];

            if (invertX)
            {
                x = 1f - x;
            }
            if (invertY)
            {
                y = 1f - y;
            }

            return new Vector2(x, y);
        }
    }
}

