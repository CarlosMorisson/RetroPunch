using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchEffect : MonoBehaviour
{
    public List<ColummTouch> FlameCollum = new();
    public static TouchEffect Instance;
    private const string FLAME_THROWER_NAME = "Flame2";
    private void Awake()
    {
        Instance = this;
    }
    /// <summary>
    /// Método principal para ativar o efeito de toque na próxima coluna disponível.
    /// </summary>
    [ContextMenu("Play Particles")]
    public void TriggerNextTouchEffect()
    {
        if (FlameCollum == null || FlameCollum.Count == 0) return;

        ColummTouch targetColumn = null;

        print("Chamou aq");
        foreach (var column in FlameCollum)
        {
            if (!column.Burned)
            {
                targetColumn = column;
                break;
            }
        }
        AudioController.Instance.Play(FLAME_THROWER_NAME);
        if (targetColumn == null)
        {
           
            foreach (var column in FlameCollum)
            {
                PlayParticles(column.LeftFlame, column.RightFlame);
            }

            return;
        }

        StartCoroutine(ExecuteColumnSequence(targetColumn));
    }

    /// <summary>
    /// Executa a sequência: Play no Flame -> Espera acabar -> Play no Touch -> Marca como Burned.
    /// </summary>
    private IEnumerator ExecuteColumnSequence(ColummTouch column)
    {
        PlayParticles(column.LeftFlame, column.RightFlame);

        float waitTime = 0.1f;
        if (column.LeftFlame != null) waitTime = Mathf.Max(waitTime, column.LeftFlame.main.duration);
        if (column.RightFlame != null) waitTime = Mathf.Max(waitTime, column.RightFlame.main.duration);

        yield return new WaitForSeconds(waitTime);

        PlayParticles(column.LeftTouch, column.RightTouch);
        column.Burned = true;
    }

    /// <summary>
    /// Reseta o estado Burned de todas as colunas e para a execução/emissão de todas as partículas.
    /// </summary>
    [ContextMenu("Reset Particles")]
    public void ResetAllColumns()
    {
        StopAllCoroutines(); 

        foreach (var column in FlameCollum)
        {
            column.Burned = false;

            StopParticles(column.LeftFlame, column.RightFlame);
            StopParticles(column.LeftTouch, column.RightTouch);
        }
    }

    /// <summary>
    /// Método auxiliar seguro para dar Play nas partículas esquerda e direita caso existam.
    /// </summary>
    private void PlayParticles(ParticleSystem left, ParticleSystem right)
    {
        if (left != null) left.Play();
        if (right != null) right.Play();
    }

    /// <summary>
    /// Método auxiliar seguro para parar as partículas esquerda e direita caso existam.
    /// </summary>
    private void StopParticles(ParticleSystem left, ParticleSystem right)
    {
        if (left != null)
        {
            left.Stop();
            //left.Clear(); 
        }
        if (right != null)
        {
            right.Stop();
            //right.Clear();
        }
    }
}

[System.Serializable]
public class ColummTouch
{
    [Header("Left")]
    public ParticleSystem LeftTouch;
    public ParticleSystem LeftFlame;
    [Header("Right")]
    public ParticleSystem RightTouch;
    public ParticleSystem RightFlame;

    public bool Burned;
}