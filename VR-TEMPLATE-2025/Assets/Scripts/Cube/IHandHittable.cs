using UnityEngine;

/// <summary>
/// Cubo que pode receber um golpe detectado fora do OnCollisionEnter (ex: HandHitAssist do hand tracking).
/// </summary>
public interface IHandHittable
{
    /// <summary>
    /// Aplica o golpe como se a mão tivesse colidido. Retorna false se o golpe foi ignorado.
    /// </summary>
    /// <param name="hitter">Objeto da mão (o mesmo que colidiria; usado pelo HandTouchFeedback).</param>
    /// <param name="handPosition">Posição da mão no instante do golpe.</param>
    /// <param name="contactPoint">Ponto do impacto na superfície do cubo.</param>
    bool TryHandAssistHit(GameObject hitter, Vector3 handPosition, Vector3 contactPoint);
}
