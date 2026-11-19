using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 13 (0i, Dan: "birds and other animals on Venus - at least hear them"). Above ground: bird song and calls from
    /// varied positions around her in the gardens (3D, never the same spot twice), distant bird and animal calls far
    /// across the valley, a soft insect bed in the grass, and now and then something small rustling through the grass
    /// near her (with a flutter of birds taking off). Underground it fades to (almost) nothing. Muted check runs log
    /// what played (<see cref="Count"/>).
    /// </summary>
    public class VenusWildlife : MonoBehaviour
    {
        public Transform Listener;
        /// <summary>0 above ground .. 1 underground (set by the director).</summary>
        public float Under;
        public int Count { get; private set; }
        AudioSource _insects;
        float _nextSong, _nextFar, _nextRustle;

        void Start()
        {
            _nextSong = Time.time + 1.5f; _nextFar = Time.time + 6f; _nextRustle = Time.time + 9f;
        }

        void Update()
        {
            if (Listener == null) { var c = Camera.main; if (c == null) return; Listener = c.transform; }
            if (_insects == null && AudioKit.Clip("insects_loop") != null) _insects = AudioKit.Loop("insects_loop", null, null, 0f, AudioKit.Bus.Ambience);
            float above = 1f - Mathf.Clamp01(Under);
            if (_insects != null) AudioKit.SetBaseVolume(_insects, 0.45f * above);
            if (above < 0.15f) return;
            var p = Listener.position;
            if (Time.time > _nextSong)
            {
                // a songbird somewhere 8-30 m away, in a tree or the grass, a little above the ground
                var dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                var at = p + dir * Random.Range(8f, 30f); at.y = Ground.Height(at.x, at.z) + Random.Range(1.5f, 7f);
                AudioKit.Play(Random.value < 0.5f ? "bird_song_a" : "bird_song_b", at, Random.Range(0.35f, 0.6f) * above, 0.12f, AudioKit.Bus.Ambience, 45f);
                Count++;
                _nextSong = Time.time + Random.Range(2.5f, 7f);
            }
            if (Time.time > _nextFar)
            {
                var dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                var at = p + dir * Random.Range(35f, 60f) + Vector3.up * Random.Range(5f, 25f);
                AudioKit.Play(Random.value < 0.6f ? "bird_call_far" : "animal_call_far", at, Random.Range(0.4f, 0.7f) * above, 0.08f, AudioKit.Bus.Ambience, 90f);
                Count++;
                _nextFar = Time.time + Random.Range(9f, 18f);
            }
            if (Time.time > _nextRustle)
            {
                // something small darts through the grass near her; sometimes birds take off from a bush
                var dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                var at = p + dir * Random.Range(3f, 9f); at.y = Ground.Height(at.x, at.z) + 0.2f;
                if (Random.value < 0.7f) AudioKit.Play("grass_rustle", at, 0.5f * above, 0.1f, AudioKit.Bus.Ambience, 18f);
                else AudioKit.Play("wing_flutter", at + Vector3.up * 1.5f, 0.55f * above, 0.1f, AudioKit.Bus.Ambience, 25f);
                Count++;
                _nextRustle = Time.time + Random.Range(10f, 22f);
            }
        }
    }
}
