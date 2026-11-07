using UnityEngine;

public class LazyFollowYawOnly : MonoBehaviour
{
    [Header("Target & Positioning")]
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float distance = 1.8f;
    [SerializeField] private float heightOffset = -0.2f;

    [Header("Follow Settings")]
    [SerializeField] private float angleThreshold = 35f;
    [SerializeField] private float followSpeed = 5f;

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // Kameranın sadece yatay (Yaw) yönünü al, yukarı/aşağı (Pitch) eğimini sıfırla
        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f) return;
        cameraForward.Normalize();

        // Panelin kameranın merkezine göre yatay yönünü bul
        Vector3 panelDirection = transform.position - targetCamera.position;
        panelDirection.y = 0f;
        panelDirection.Normalize();

        // Kamera ile panel arasındaki yatay açıyı ölç
        float angleDiff = Vector3.Angle(cameraForward, panelDirection);

        // Hedef konumu hesapla (Kameranın dünya yüksekliği + ofset)
        Vector3 targetPos = targetCamera.position + (cameraForward * distance);
        targetPos.y = targetCamera.position.y + heightOffset;

        // Hedef rotasyon (Sadece Y ekseninde kullanıcıya dönük, zemine paralel)
        Quaternion targetRot = Quaternion.LookRotation(cameraForward, Vector3.up);

        // Açı eşiği aşıldıysa paneli yumuşakça hedefe taşı
        if (angleDiff > angleThreshold)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
        }
        else
        {
            // Açı eşiği içinde kalsa bile yükseklik ve mesafe kaymasını yavaşça dengede tut
            transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, targetPos.y, transform.position.z), Time.deltaTime * followSpeed);
        }
    }

    public void SnapToTarget()
    {
        if (targetCamera == null) return;

        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 targetPos = targetCamera.position + (cameraForward * distance);
        targetPos.y = targetCamera.position.y + heightOffset;

        transform.SetPositionAndRotation(targetPos, Quaternion.LookRotation(cameraForward, Vector3.up));
    }
}