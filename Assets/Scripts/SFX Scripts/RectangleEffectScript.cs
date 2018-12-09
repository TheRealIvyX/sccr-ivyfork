﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rectangle effect script used to create the cool rectangles
/// </summary>
public class RectangleEffectScript : MonoBehaviour {

    public ParticleSystem partSys; // particle system that stores the particles
    private Vector3 displacement; // used to wrap particles around

    // Use this for initialization
    private void Start () {
        var sh = partSys.shape; // grab the shape of the particle system
        Vector3 dimensions = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight, 
            sh.position.z - Camera.main.transform.position.z));
        // gets the dimensions of the screen the game is playing on in x and y values
        sh.scale = new Vector3(dimensions[1]*2, dimensions[0]*2, 0); // WHY *2?????????
        // scales up the emitters so that particles are uniformly emitted across the screen, I suspect the scale is halved for some reason
        partSys.Emit(25);
    }

    /// <summary>
    /// Updater for the particles in the field, wraps them around if necessary
    /// </summary>
    /// <param name="particles">array of particles in the field</param>
    private void ParticleUpdate(ParticleSystem.Particle[] particles) {
        for (int i = 0; i < particles.Length; i++) // update all particles
        {
            ParticleWrapper(ref particles[i], 0); // call particle wrapper for both dimensions
            ParticleWrapper(ref particles[i], 1);
        }
    }

    /// <summary>
    /// particleUpdate helper method: wraps the particles to the other side of the screen if they are not in the correct bounds
    /// </summary>
    /// <param name="particle">the particle to wrap</param>
    /// <param name="dimension">the dimension (0 is x, 1 is y)</param>
    private void ParticleWrapper(ref ParticleSystem.Particle particle, int dimension)
    {
        float limit; // wrapping limit
        limit = dimension == 0 ? Camera.main.pixelWidth : Camera.main.pixelHeight; 
        // get the limit based on dimension

        Vector3 relativeCameraPos = Camera.main.WorldToScreenPoint(particle.position);
        // grab the screen position of the particle
        if (relativeCameraPos[dimension] < 0 || relativeCameraPos[dimension] > limit) {
            // if the particle is passed the screen limits wrap it around
            displacement = relativeCameraPos;
            displacement[dimension] = Mathf.Abs(displacement[dimension] - limit);
            particle.position = Camera.main.ScreenToWorldPoint(displacement);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[25]; // constantly update particle array, room for optimization here
            partSys.GetParticles(particles); // get particles
            ParticleUpdate(particles);
            partSys.SetParticles(particles, 25); // set particles
    }
}