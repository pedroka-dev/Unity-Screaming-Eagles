using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public static class UnityExtensions
{
    /// <summary>
    /// LayerMask Extension method to check if a Layer is in a Layermask. 
    /// </summary>
    /// <param name="mask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool Contains(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }


    /// <summary>
    /// AudioSource Extension method that plays OneShot a random AudioClip from a List of AudioClips.
    /// </summary>
    /// <param name="audioSource"></param>
    /// <param name="audioClips"></param>
    public static void PlayOneShotRandom(this AudioSource audioSource, List<AudioClip> audioClips, float volumeScale = 1f)
    {
        int critAudioId = Random.Range(0, audioClips.Count - 1);
        audioSource.PlayOneShot(audioClips.ElementAt(critAudioId),volumeScale);
    }

    /// <summary>
    /// Courotine for hiding and deleting the an object once a expecific audio source stopped playing. Checked every frame. 
    /// </summary>
    /// <returns></returns>
    public static IEnumerator DestroyAfterAudioAndPaticlesEnd(this GameObject gameObject, AudioSource audioSource, ParticleSystem particleSystem)
    {
        gameObject.GetComponent<Renderer>().enabled = false;    //Hides the object from view without stopping the audioclip
        while (audioSource.isPlaying || particleSystem.isPlaying)
        {
            yield return new WaitForEndOfFrame();
        }
        Object.Destroy(gameObject);
    }
}
