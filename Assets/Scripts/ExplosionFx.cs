using UnityEngine;

public static class ExplosionFx
{
    private static Material _particleMat;

    private static Material ParticleMaterial
    {
        get
        {
            if (_particleMat != null)
            {
                return _particleMat;
            }

            Shader[] candidates = new Shader[]
            {
                Shader.Find ("Particles/Standard Unlit"),
                Shader.Find ("Legacy Shaders/Particles/Alpha Blended"),
                Shader.Find ("Mobile/Particles/Alpha Blended"),
                Shader.Find ("Universal Render Pipeline/Particles/Unlit"),
            };

            foreach (Shader s in candidates)
            {
                if (s != null)
                {
                    _particleMat = new Material (s);
                    return _particleMat;
                }
            }

            return null;
        }
    }

    private static void ApplyRendererDefaults (ParticleSystemRenderer rend)
    {
        if (rend == null)
        {
            return;
        }

        rend.material = ParticleMaterial;
        rend.sortingLayerName = "Default";
        rend.sortingOrder = 95;
    }

    private static Gradient FadeOutGradient ()
    {
        Gradient g = new Gradient ();
        g.SetKeys (
            new[] { new GradientColorKey (Color.white, 0f), new GradientColorKey (Color.white, 1f) },
            new[] { new GradientAlphaKey (1f, 0f), new GradientAlphaKey (0f, 1f) });
        return g;
    }

    public static void PlayMagicExplosion (Vector3 position, float worldRadius)
    {
        if (ParticleMaterial == null)
        {
            return;
        }

        GameObject go = new GameObject ("FX_MagicExplosion");
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem> ();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.07f;
        main.playOnAwake = false;
        float scale = Mathf.Clamp (worldRadius * 0.55f, 0.4f, 2.4f);
        main.startLifetime = 0.32f + scale * 0.06f;
        main.startSpeed = new ParticleSystem.MinMaxCurve (2f * scale, 5f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve (0.07f * scale, 0.2f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient (
            new Color (0.58f, 0.38f, 1f, 1f),
            new Color (0.35f, 0.88f, 1f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.07f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        short count = (short) Mathf.Clamp (32 + worldRadius * 10f, 32, 64);
        emission.SetBursts (new[] { new ParticleSystem.Burst (0f, count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.07f * scale;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeOutGradient ();

        ApplyRendererDefaults (go.GetComponent<ParticleSystemRenderer> ());
        ps.Play ();
        Object.Destroy (go, 1.75f);
    }

    public static void PlayEnemyDeath (Vector3 position)
    {
        if (ParticleMaterial == null)
        {
            return;
        }

        GameObject go = new GameObject ("FX_EnemyDeath");
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem> ();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.06f;
        main.playOnAwake = false;
        main.startLifetime = 0.38f;
        main.startSpeed = new ParticleSystem.MinMaxCurve (1.4f, 3.6f);
        main.startSize = new ParticleSystem.MinMaxCurve (0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient (
            new Color (1f, 0.45f, 0.18f, 1f),
            new Color (1f, 0.82f, 0.35f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.35f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts (new[] { new ParticleSystem.Burst (0f, (short) 26) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeOutGradient ();

        ApplyRendererDefaults (go.GetComponent<ParticleSystemRenderer> ());
        ps.Play ();
        Object.Destroy (go, 1.35f);
    }

    public static void PlayImpactBurst (Vector3 position)
    {
        if (ParticleMaterial == null)
        {
            return;
        }

        GameObject go = new GameObject ("FX_Impact");
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem> ();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.05f;
        main.playOnAwake = false;
        main.startLifetime = 0.18f;
        main.startSpeed = new ParticleSystem.MinMaxCurve (2.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve (0.04f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient (
            new Color (1f, 0.85f, 0.35f, 1f),
            new Color (1f, 0.55f, 0.15f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts (new[] { new ParticleSystem.Burst (0f, (short) 12) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeOutGradient ();

        ApplyRendererDefaults (go.GetComponent<ParticleSystemRenderer> ());
        ps.Play ();
        Object.Destroy (go, 0.85f);
    }
}
