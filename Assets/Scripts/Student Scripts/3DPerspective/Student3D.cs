using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Student3D : Student
{
    [SerializeField] private Animator animator;
    [SerializeField] private StudentVFX studentVFX;

    private bool _resting = false;
    private float _workingMultiplier = 1.0f;
    private bool _distracted = false;
    private bool _burnedOut = false;
    private bool _failed = false;
    private bool _victory = false;
    private bool _isDragged = false;

    public bool Resting { get => _resting; set {_resting = value; animator?.SetBool("Resting", value); } }
    public float WorkingMultiplier { get => _workingMultiplier; set {_workingMultiplier = value; animator?.SetFloat("WorkingMultiplier", value); } }
    public bool Distracted { get => _distracted; set {_distracted = value; animator?.SetBool("Distracted", value); } }
    public bool BurnedOut { get => _burnedOut; set {_burnedOut = value; animator?.SetBool("BurnedOut", value); } }
    public bool Failed { get => _failed; set {_failed = value; animator?.SetBool("Failed", value); } }
    public bool Victory { get => _victory; set {_victory = value; animator?.SetBool("Victory", value); } }
    public bool IsDragged { get => _isDragged; set {_isDragged = value; animator?.SetBool("IsDragged", value); } }

    private Plane dragPlane;
    private Vector3 dragOffset;
    private Camera mainCamera;

    private StudentState lastState;

    [Header("Efecto de Arrastre")]
    public float alturaDeVuelo = 1.5f;

    public StudentVFX GetStudentVFX() {
        return this.studentVFX;
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        studentVFX = GetComponent<StudentVFX>();
        dragPlane = new Plane(Vector3.up, Vector3.zero);
        Working();
    }

    public void FailPartial() {
        animator.SetTrigger("FailPartial");
    }

    public void PartialPassed() {
        animator.SetTrigger("PartialPassed");
    }

    public void Grounded() {
        //animator.ResetTrigger("Grounded", true);
        animator.SetTrigger("Grounded");
    }

    private void Working() {
        Resting = false;
        WorkingMultiplier = 1.0f;
        Distracted = false;
        IsDragged = false;
        BurnedOut = false;
    }

    public void Dragged()
    {
        IsDragged = true;
        Resting = false;
        WorkingMultiplier = 1.0f;
        Distracted = false;
        BurnedOut = false;

    }

    public override void ChangeState(StudentState newState){
        StudentState currState = currentState;
        base.ChangeState(newState);
        studentVFX.DeactivateAllParticles();
        switch (newState)
        {
            case StudentState.Working:
                Working();
                if (currState == StudentState.Distracted) {
                    Grounded();
                }
                break;
            case StudentState.Flow:
                WorkingMultiplier = 1.5f;
                break;
            case StudentState.Burnout:
                Working();
                BurnedOut = true;
                studentVFX.ActivateFire();
                break;
            case StudentState.Resting:
                Working();
                Resting = true;
                break;
            case StudentState.DroppedOut:
                Working();
                Failed = true;
                break;
            case StudentState.Distracted:
                Working();
                Distracted = true;
                break;
            case StudentState.Finished:
                Working();
                PartialPassed();    
                studentVFX.ActivateFinished();
                break;
            case StudentState.Graduated:
                Working();
                Victory = true;
                break;
            default:
                Working();
                break;
        }
   
    }

    public override void OnBeginDrag(PointerEventData eventData)  { 
        Debug.Log("OnBeginDrag");
        originalPosition = transform.position; 
        lastState = currentState;
        Dragged();
        
        // 1. Creamos un piso matemático invisible exactamente a la altura del alumno
        dragPlane = new Plane(Vector3.up, transform.position);

        // 2. Calculamos de dónde lo agarramos para que no salte al centro del mouse
        Ray camRay = Camera.main.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(camRay, out float distance))
        {
            dragOffset = transform.position - camRay.GetPoint(distance);
        }
    }
    
    public override void OnDrag(PointerEventData eventData) {
        if (IsDragged) {
            Ray camRay = Camera.main.ScreenPointToRay(eventData.position);
            if (dragPlane.Raycast(camRay, out float distance))
            {
                // ¡LA MAGIA!: Lo mantenemos atado al mouse, pero flotando a la altura deseada
                transform.position = camRay.GetPoint(distance) + dragOffset + (Vector3.up * alturaDeVuelo);
            }
        }
    }

    public override void OnEndDrag(PointerEventData eventData) {
        IsDragged = false; // <--- ¡APAGAMOS LA ANIMACIÓN AL SOLTARLO!
        bool dragExitoso = false;
        float snapRadius = 3f; // Quizás en 3D necesites ajustar este número un poco
        Seat[] todasLasSillas = FindObjectsByType<Seat>(FindObjectsSortMode.None);        
        Seat sillaMasCercana = null;
        float distanciaMinima = float.MaxValue;

        foreach (Seat silla in todasLasSillas)
        {
            // Aplanamos las posiciones ignorando la altura (Y) para que el imán funcione perfecto aunque esté volando
            Vector3 posicionPlanaAlumno = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 posicionPlanaSilla = new Vector3(silla.transform.position.x, 0, silla.transform.position.z);

            float distancia = Vector3.Distance(posicionPlanaAlumno, posicionPlanaSilla);
            
            if (distancia < distanciaMinima && distancia <= snapRadius)
            {
                distanciaMinima = distancia;
                sillaMasCercana = silla;
            }
        }

        // ... (El resto de tu lógica de intercambiar sillas se queda exactamente igual)
        if (sillaMasCercana != null)
        {
            if (sillaMasCercana.currentStudent != null && sillaMasCercana.currentStudent != this)
            {
                Seat miSillaVieja = this.currentSeat;
                Seat suSillaVieja = sillaMasCercana;
                Student elOtroAlumno = sillaMasCercana.currentStudent;

                if (miSillaVieja != null)
                {
                    suSillaVieja.AssignStudent(this);
                    miSillaVieja.AssignStudent(elOtroAlumno);
                    dragExitoso = true;
                }
            }
            else if (sillaMasCercana.currentStudent == null)
            {
                Seat miSillaVieja = this.currentSeat;
                if (miSillaVieja != null) miSillaVieja.currentStudent = null; 

                sillaMasCercana.AssignStudent(this); 
                dragExitoso = true;
                ChangeState(lastState); // Restauramos el estado anterior del alumno
            }
        }

        if (dragExitoso == false)
        {
            if (currentSeat != null) transform.position = currentSeat.transform.position;
            else transform.position = originalPosition;
        }
    }
}
