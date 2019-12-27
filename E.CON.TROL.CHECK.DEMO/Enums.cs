namespace E.CON.TROL.CHECK.DEMO
{
    public enum BoxCheckStates : int
    {
        /// <summary>
        /// Kiste ist noch nicht geprueft
        /// </summary>
        RAW = 0,
        /// <summary>
        /// Pruefung lauft gerade
        /// </summary>
        ACTIVE = 1,
        /// <summary>
        /// Kiste ist in Ordnung
        /// </summary>
        IO = 10,
        /// <summary>
        /// Kiste ist fehlerhaft
        /// </summary>
        NIO = 20,
        /// <summary>
        /// Pruefung wurde abgebrochen
        /// </summary>
        TIMEOUT = 30,
        /// <summary>
        /// FEHLER
        /// </summary>
        ERROR = 40,
        /// <summary>
        /// Pruefung wurde Sicherheitshalber abgeschaltet
        /// </summary>
        SAFETYMODE = 50
    }

    /// <summary>
    /// Auflistung der Ausschleusegruende
    /// </summary>
    public enum BoxFailureReasons : int
    {
        /// <summary>
        /// Behaelter ist in Ordnung
        /// </summary>
        BOX_FAILURE_NONE = 0,
        /// <summary>
        /// Behaelter ist fehlerhaft (unbekannter Grund)
        /// </summary>
        BOX_FAILURE_UNKNOWN = 1,
        /// <summary>
        /// Behaelter hat einen fehlerhaften Barcode
        /// </summary>
        BOX_CODE_DEFECT = 2,
        /// <summary>
        /// Behaelter hat Absplitterungen an der Aussenseite
        /// </summary>
        BOX_CHIPPING_OUTSIDE = 10,
        /// <summary>
        /// Behaelter hat Verschutzungen (Etiketten an der Innenseite)
        /// </summary>
        BOX_DIRT_LABEL_INSIDE = 20,
        /// <summary>
        /// Behaelter hat Verschutzungen (Etiketten an der Aussenseite)
        /// </summary>
        BOX_DIRT_LABEL_OUTSIDE = 21,
        /// <summary>
        /// Behalter hat einen fehlerhaften Verschluss
        /// </summary>
        BOX_DEFECT_CLOSURE = 30,
        /// <summary>
        /// Seitenteil ist offen
        /// </summary>
        BOX_FRAME_SIDE_OPEN = 31,
        /// <summary>
        /// Scharnier auf Außenseite ist ausgebrochen
        /// </summary>
        BOX_JAILER_DEFECT = 40,
        /// <summary>
        /// Clip für Labels ist defekt
        /// </summary>
        BOX_CLIP_DEFECT = 50,
        /// <summary>
        /// Pruefung konnte aufgrund von falscher Bandgeschwindigkeit nicht durchgefuehrt werden
        /// </summary>
        BOX_FAILURE_VELOCITY = 99
    }
}
