# Last Fleet – Spielstand und Featureübersicht

Stand: 18. November 2026 (Commit `6610a08`, „Add win and lose screens with a scored run account and saved
run history“).

Dieses Dokument beschreibt, was im Prototyp heute tatsächlich spielbar ist. Zahlen sind die aktuellen
Balancing-Werte aus dem Code und können sich ändern. Technische Details und die mit dem Designer
vereinbarten Entscheidungen stehen in den READMEs der Module:
[Combat](Assets/Modules/Combat/README.md), [Quests](Assets/Modules/Quests/README.md),
[Repairs](Assets/Modules/Repairs/README.md), [TacticalWeapons](Assets/Modules/TacticalWeapons/README.md),
[StrandedShips](Assets/Modules/StrandedShips/README.md), [LateSignals](Assets/Modules/LateSignals/README.md),
[FleetModules](Assets/Modules/FleetModules/README.md), [Tutorial](Assets/Modules/Tutorial/README.md),
[AbandonedShips](Assets/Modules/AbandonedShips/README.md), [Hud](Assets/Modules/Hud/README.md) und
[RunEnd](Assets/Modules/RunEnd/README.md).

---

## 1. Das Spiel in Kürze

**Last Fleet** ist ein Echtzeit-Strategie- und Überlebensspiel im Weltraum. Der Spieler führt die letzte
Flotte der Menschheit: einige große Großkampfschiffe voller Flüchtlinge und eine Handvoll kleiner
Arbeits-, Transport- und Kampfschiffe. Die Flotte flieht durch eine zufällig erzeugte Galaxie vor den
**Pronin**, einer feindlichen Macht, die ihr von System zu System folgt.

Das Spiel kennt **keine Pause**. Die Uhr läuft immer weiter, auch Dialoge sind Funksprüche, während das
Geschehen weiterläuft. Das Spiel läuft sogar weiter, wenn das Fenster den Fokus verliert.

### Der Kernkreislauf

1. **Ankommen.** Die Flotte springt in ein neues Sternsystem und erscheint am Sprungpunkt. Ab sofort läuft
   ein Countdown von 13 Minuten, bis die Pronin eintreffen.
2. **Erkunden.** Das System besteht aus Zonen (Planeten, Monde, Asteroidengürtel, Stationen). Unbekannte
   Zonen müssen erst von einem Schiff besucht werden, bevor man sieht, was darin liegt.
3. **Ausbeuten.** Harvester bauen Erz, Eis und Gas ab, das als Metall, Wasser und Treibstoff in die Lager
   der Flotte geht. Shuttles scannen Asteroiden und Stationen, docken an, durchsuchen verlassene Stationen
   und holen Überlebende und Bewohner an Bord. Gestrandete Großkampfschiffe und verlassene kleine Schiffe
   lassen sich reparieren bzw. neu bemannen und verstärken die Flotte.
4. **Späte Signale abwägen.** Während der Countdown läuft, tauchen neue Orte auf: Notrufe, Stationen, die
   ihr Funkschweigen brechen, und Wrackfelder. Sie sind wertvoller als das, was bei der Ankunft bekannt
   war, kommen aber erst spät. Bleiben lohnt sich, kostet aber Zeit.
5. **Überleben.** Gefahren beim Abbau, verstrahlte oder feindliche Stationen und Pronin-Jäger kosten
   Schiffe und Menschenleben. Die Menschen brauchen Wasser, die Schiffe Treibstoff.
6. **Weiterspringen.** Nach der Abkühlzeit des großen Sprungantriebs wählt der Spieler ein Ziel in
   Reichweite. Jedes Großkampfschiff bezahlt den Sprung aus dem eigenen Tank. Wer nicht zahlen kann oder
   als kleines Schiff nicht in einem Hangar sitzt, bleibt zurück und ist verloren.

Spätestens wenn der Countdown abläuft, taucht ein **Pronin-Träger** auf, der Jägerwellen startet und die
Flotte durch das System jagt. Wer zu lange bleibt, verliert immer mehr.

### Ablauf und Ziel eines Runs

Jeder Run beginnt mit einem Funkspruch, der einen optionalen **Prolog** anbietet. Er bringt im Startsystem
ohne Zeitdruck Erkunden, Andocken, Durchsuchen, Abbau und einen ersten Kampf bei und endet mit dem ersten
Sprung.

Die Questkette **„Meltdown“** zieht sich danach über fünf Sternsysteme. Sie endet damit, dass die Flotte
einen Stern zur Supernova bringt und rechtzeitig aus dem System springen muss. Gelingt das, ist der Run
**gewonnen**. Verloren ist er, wenn die Flotte in der Supernova bleibt oder ihr **letztes Großkampfschiff
zerstört** wird. Ein Endbildschirm zeigt dann eine Wertung mit Punkten, vergleicht sie mit früheren Runs,
die auf der Festplatte gespeichert werden, und ein neuer Run mit einer neuen Galaxie kann beginnen.

### Was den Ton bestimmt

- **Menschen sind die eigentliche Währung.** Jeder Rumpfschaden an einem bewohnten Schiff tötet einen Teil
  der Menschen an Bord. Kleine Schiffe brauchen Besatzung, die aus den Passagieren kommt.
- **Wissen ist unvollständig.** Asteroiden zeigen ihre Ressource und Gefahr erst nach einem Scan,
  Stationen verbergen Atmosphäre, Strahlung, Bewohner und Haltung, bis man scannt, anfunkt oder andockt.
- **Zeitdruck statt Pause.** Jede Entscheidung kostet Zeit, und Zeit bringt die Pronin näher.

### Darstellung

Alle Grafiken sind **Blockout**: Schiffe, Stationen und Asteroiden sind aus einfachen Unity-Grundformen
gebaut und farblich nach Rolle unterschieden. Gespielt wird derzeit in einer **Preview-Szene**, die
Galaxie, Systeme und Zonen erzeugt und Entwickler-Tasten zum Testen anbietet.

---

## 2. Welt und Generierung

### Galaxie
- Etwa **120 Sternsysteme** in einer flachen Scheibe mit rund **400 Lichtjahren** Radius und 30 Lichtjahren
  Dicke, mit Mindestabstand von 15 Lichtjahren zwischen den Systemen.
- Alles wird aus einem **Seed** erzeugt; derselbe Seed ergibt dieselbe Galaxie.
- Jedes System bekommt einen generierten **Namen**.
- Die **Galaxiekarte** zeigt die Systeme, das aktuelle System der Flotte, die Sprungreichweite von
  **50 Lichtjahren** und die Verbindungslinien. Systeme mit wartenden Questschritten werden markiert.

### Sternsysteme
- **Sternklassen:** Roter Zwerg, Oranger Zwerg, Gelber Zwerg, Weißer Stern, Blauer Riese.
- **Planeten:** Gestein, Wüste, Ozean, Eis, Gasriese, Eisriese – mit Monden auf Umlaufbahnen.
- **Asteroidengürtel** mit unterschiedlicher Zusammensetzung.
- **Stationen** im Orbit oder im tiefen Raum (1–4 pro System).
- In etwa **jedem fünften System** ein **gestrandetes Großkampfschiff** als eigener Ort (siehe Abschnitt 7).
- Ein **Sprungpunkt** am Systemrand: eine leere Ankunftszone, in der die Flotte erscheint.
- **0–2 späte Orte**, die erst während des Countdowns auftauchen (siehe „Späte Signale“ unten).
- Die **Systemkarte** zeigt alle bekannten Orte, den Stationstyp bereits erkundeter Zonen und markiert die
  Zone einer Queststation. Gestrandete Schiffe sind als rostroter Splitter eingezeichnet.

### Zonen
- Jeder Ort im System ist eine **Zone** mit 50 km Radius (1 Einheit = 100 m), mit einer Hintergrundkulisse
  aus Stern, Planet oder Mond.
- Inhalte je nach Ort:
  - **Asteroidengürtel:** 30–60 Vorkommen.
  - **Monde:** 4–10 Vorkommen.
  - **Planeten:** Vorkommen passend zum Planetentyp (z. B. Gas bei Gasriesen).
  - **Stationen:** die Station selbst, umgeben von 3–8 Trümmern.
  - **Gestrandete Schiffe:** das Wrack eines Großkampfschiffs, umgeben von 2–4 Rumpfteilen mit je
    60–140 Metall.
  - **Wrackfelder:** treibende Wracks und ein Derelikt (siehe „Späte Signale“).
  - **Verlassene kleine Schiffe** ohne Besatzung treiben in Wrackfeldern (60 %, 1–2 Schiffe), bei
    gestrandeten Schiffen (25 %) und bei Stationen (25 %, je 1 Schiff), siehe Abschnitt 4.
  - Jedes System enthält garantiert ein **Treibstoff-Vorkommen**.
- Unbekannte Zonen müssen **erkundet** werden: Ein Schiff fliegt hin, erst dann wird der Inhalt erzeugt und
  sichtbar.
- Mit **Tab / Shift+Tab** wechselt man zwischen bereits aufgedeckten Zonen.
- Der Zustand jeder Zone (Scans, abgebaute Mengen, Wracks, Stationszustand) bleibt für den ganzen Run
  erhalten.

### Späte Signale
- Jedes System hat **0–2 späte Orte** (keiner 20 %, einer 55 %, zwei 25 %). Sie werden mit dem System
  erzeugt, sind aber auf der Karte versteckt, bis zwischen **40 % und 90 % des Pronin-Countdowns**
  vergangen sind (etwa nach 5–12 Minuten).
- Arten:
  - **Notruf eines gestrandeten Schiffs** (40 %): Es hat noch 40–75 % Rumpf statt 20–50 % und immer
    Überlebende an Bord. Der Notruf öffnet einen **Funkspruch**: „Shuttle schicken“ schickt ein freies
    kleines Schiff hin, „Ignorieren“ tut nichts. Ohne Antwort wird der Ruf nach **45 Sekunden** Echtzeit
    ignoriert. Der Ort bleibt in jedem Fall auf der Karte.
  - **Station, die ihr Funkschweigen bricht** (40 %): nie feindlich, nie eine Ruine, beim Durchsuchen
    **anderthalbmal so viele Funde**.
  - **Wrackfeld** (20 %, gibt es nur als spätes Signal): **8–14 treibende Wracks mit je 100–300 Metall**,
    ungescannt. Etwa ein Drittel birgt eine Gefahr: ein leckender Reaktor (radioaktiv) oder Treibstoffreste
    (Gastaschen). In der Mitte liegt ein **Derelikt**, ein in zwei Teile zerbrochener Kreuzer, der wie eine
    verlassene Station durchsucht wird (4–8 Funde). Etwa ein Viertel davon ist verstrahlt oder giftig und
    bleibt verschlossen.
- Empfangene Signale verfallen nicht und bleiben für den Run bekannt. Nicht empfangene warten auf den
  nächsten Countdown in diesem System. Queststationen liegen nie an späten Orten.
- Das Flottenmodul **Signalverstärker** lässt die Signale doppelt so früh kommen.

---

## 3. Steuerung

### Kamera
- RTS-Kamera: **WASD** oder **Pfeiltasten** verschieben, **Q / E** drehen, **Mausrad** zoomt,
  **Shift** beschleunigt die Bewegung.
- **F** folgt dem ausgewählten Schiff.

### Auswahl und Befehle
- **Linksklick** wählt ein Schiff bzw. sein Squad aus (getroffen wird das sichtbare Modell),
  **Doppelklick** alle Schiffe dieser Klasse in der Zone, **Aufziehen** eines Rahmens alle Schiffe darin.
  **Shift** fügt zur Auswahl hinzu. Rechtsklick auf leeren Raum bewegt die Auswahl in Formation.
- **Rechtsklick** gibt kontextabhängige Befehle:
  - auf leeren Raum: dorthin fliegen,
  - auf ein Vorkommen mit Harvestern: abbauen,
  - auf ein ungescanntes Vorkommen oder eine Station mit einem Shuttle: scannen,
  - auf eine Station mit kleinen Schiffen: andocken (ein Shuttle scannt eine ungescannte Station zuerst),
  - auf einen Feind: angreifen,
  - mit Jägern auf ein eigenes Schiff: eskortieren,
  - mit kleinen Schiffen auf ein kleines Schiff ohne Besatzung: eine Person hinüberschicken (siehe
    Abschnitt 4, „Verlassene Schiffe und Crew-Übergabe“).
- **Shift** hängt Befehle an eine Warteschlange an (z. B. „starten, fliegen, zurückkehren“).
- Ein neuer Befehl ersetzt laufende Automatik wie Abbau, Eskorte oder Evakuierung.

### Gruppen, Squads und Hangars
- **Kontrollgruppen:** **Strg + Ziffer** speichert die Auswahl, **Ziffer** wählt sie, doppeltes Drücken
  springt mit der Kamera hin.
- **Squads:** **G** bildet aus der Auswahl ein Squad (nur Schiffe derselben Klasse). Squads fliegen in
  Formation und haben einen Anführer.
- **Hangars:** **H** schickt die Auswahl in ihren Hangar zurück, **Strg + H** alle Schiffe, **L** startet
  die ausgewählten Schiffe. Ein Hangar startet ein Schiff pro Sekunde.
- **Zonensprünge:** Befehle in eine andere Zone lassen die Schiffe dorthin springen. Das kostet Treibstoff
  pro AE und hat einen kurzen Countdown.

### Fenster und Anzeigen
- **Inspektionsfenster** (oben rechts) für Vorkommen, Stationen und Orte der Systemkarte. Es zeigt nur, was
  bekannt ist, aktualisiert sich live und bietet passende Aktionen an. Buttons nennen ihre Tastenkürzel,
  gesperrte Buttons sagen, warum sie gesperrt sind. Die Schiffsauswahl bleibt erhalten, sodass man sie
  direkt auf das Ziel ansetzen kann.
- **Schiffsfenster** (unten rechts) für die Auswahl: Rumpf, Schild, Treibstoff, Besatzung und Effizienz,
  Passagiere und Überbelegung, Aufgabe, Bewegung, Zone, Squad, Ladung, Funde an Bord, Hangar und Lager.
  Bei mehreren Schiffen eine Zeile pro Schiff. Aktionen: zurück in den Hangar, starten, Squad bilden,
  Ladung abwerfen, reparieren, betanken, Auto-Tanken, EMP-Rakete abfeuern und bauen.
- **Flottenmodule:** Ein Button oben in der Mitte zeigt, wie viele Module zur Wahl bereitstehen. **U** öffnet
  das Fenster (siehe Abschnitt 4).
- **Erfolgsmeldungen** (oben Mitte): kurze Einblendungen für kleine Erfolge, z. B. gerettete Menschen, ein
  neues Schiff, wieder genug Wasser, eine von Feinden befreite Zone, gesicherte Funde, ein abgebautes
  Vorkommen. Bis zu 3 gleichzeitig, je 4 Sekunden. Sie fangen keine Klicks ab.
- **Prolog-Ziel** (oben Mitte, nur im Prolog): aktueller Schritt und Ziel, „Schritt überspringen“ und
  „Prolog beenden“.
- **Versorgungsleiste** (oben): Treibstoff, Metall und Wasser im Lager im Verhältnis zur Kapazität, dazu wie
  lange das Wasser noch reicht.
- **Bedrohungsanzeige** (oben): der Pronin-Countdown, 30 s vor Ablauf rot.
- **Sprungstatus:** Abkühlzeit des großen Sprungantriebs, danach ein Button „Sprung bereit“.
- **Quest-Log** (unten Mitte): aktiver Schritt, Ziel, System, Zone und Live-Status, dazu angenommene
  Stationsaufträge.
- **Dialogfenster** (unten Mitte) für Funksprüche mit Antwort-Buttons.
- **Meldungen:** Die letzten Ereignisse (Verluste, Funde, Warnungen) bleiben sichtbar.

---

## 4. Die Flotte

### Startaufstellung

| Schiff | Rolle | Besonderheiten |
| --- | --- | --- |
| **Raffinerie** | Industrie-Großkampfschiff | Lager für Metall (30.000) und Wasser, Landebucht für Harvester, 800 von 1.200 Wohnplätzen belegt, startet mit 2.500 Wasser, Rumpf 2.000, Tank 5.000 |
| **Linienschiff** | Ziviles Großkampfschiff (ca. 1,6 km) | 6.000 von 8.000 Wohnplätzen belegt, Hangar mit 6 Plätzen, Rumpf 2.500, Tank 4.000 |
| **Tanker** | Industrie-Großkampfschiff ohne Hangar | Treibstofflager 20.000 (Start: 8.000), betankt auf Befehl Großkampfschiffe in seiner Zone |
| **Kreuzer** | Militär-Großkampfschiff | Rumpf 3.500, Geschütztürme mit 360° Feuerbereich, Hangar mit 8 Plätzen |
| **2 Harvester-Squads à 3** | Abbau | Rumpf 100, Tank 100, Laderaum 120, Besatzung 3 |
| **1 Shuttle-Squad à 2** (im Linienschiff) | Transport und Aufklärung | Rumpf 80, Tank 120, 40 Sitze, Besatzung 2, schneller Scan (2 s) |
| **2 Jägerstaffeln à 4** (im Kreuzer) | Kampf | Rumpf 60, Schild 60, Besatzung 2 |

Schiffe tragen lesbare, übersetzbare Namen wie „Sammelschiff 3“ oder „Harvester 3“.

### Rumpf und Zerstörung
- Jedes Schiff hat **Rumpfpunkte**. Bei null ist es endgültig zerstört und wird aus Auswahl, Squads,
  Kontrollgruppen und Abbauplätzen entfernt.
- Schiffe, die in einem zerstörten Träger angedockt sind, gehen mit ihm verloren. Angedockte Schiffe nehmen
  selbst keinen Schaden.
- Verluste werden mit Ursache gemeldet, z. B. „verloren mit N Menschen an Bord“.

### Reparatur
- **Nichts wird automatisch repariert.** Der Spieler gibt jede Reparatur im Schiffsfenster in Auftrag
  („Reparieren“).
- Jedes Großkampfschiff hat eine **Werft**. Ihre Ingenieure arbeiten immer nur an **einem** Schiff: am
  Großkampfschiff selbst oder an einem kleinen Schiff in seinem Hangar. Die Reihenfolge legt der Spieler
  fest („Zuerst reparieren“, „Reparatur abbrechen“).
- **Kleine Schiffe** werden nur im Hangar repariert. Startet ein Schiff, fällt es aus der Warteschlange.
- Kosten und Tempo: **0,5 Metall pro Rumpfpunkt**, kleine Schiffe 2 Punkte/s, Großkampfschiffe 10 Punkte/s.
  Die Reparatur läuft auch im Kampf weiter. Ohne Metall pausiert sie und läuft danach von selbst weiter.
- Repariert wird nur der Rumpf. Tote bleiben tot, zerstörte Schiffe bleiben verloren.

### Treibstoff
- Alle Schiffe starten mit vollem Tank.
- **Verbrauch** pro Kilometer, solange Hauptantrieb oder Manövrierdüsen laufen. Antriebsloses Gleiten
  kostet nichts.
- **Zonensprünge** kosten Treibstoff pro AE und werden beim Start des Sprung-Countdowns voll bezahlt. Reicht
  der Treibstoff nicht, wird der Befehl verweigert und das Schiff bremst ab.
- Mit **leerem Tank** fällt der Hauptantrieb aus. Das Schiff kriecht auf den Manövrierdüsen weiter (die aus
  einer Reserve laufen) und kann so noch einen Hangar in seiner Zone erreichen.
- **Betankung kleiner Schiffe:** Nach dem Landen tanken sie aus dem Tank des Schiffs, auf dem sie gelandet
  sind (25 pro Sekunde). Harvester tanken zusätzlich beim Abladen an der Landebucht auf, aus dem Tank des
  Schiffs, bei dem sie abladen.
- **Automatisch zum Tanken:** Fällt ein kleines Schiff unter seine Schwelle, unterbricht es seine Aufgabe, auch
  im Kampf, und landet im nächsten Hangar mit Platz (bevorzugt in der eigenen Zone). Vollgetankt macht es weiter:
  Harvester bauen am selben Vorkommen weiter, Jäger eskortieren oder greifen wieder an, Shuttles setzen ihre
  Evakuierung fort, offene Flugbefehle werden erneut gegeben. Ohne Aufgabe bleibt es im Hangar.
  - Die Schwelle stellt der Button **„Auto-Tanken“** im Schiffsfenster ein: Aus → 15 % → 30 % → 50 %. Alle
    kleinen Schiffe starten mit **30 %**. Bei Mehrfachauswahl gilt der Klick für alle ausgewählten Schiffe (die
    nächste Stufe nach dem ersten Schiff), unterschiedliche Werte zeigt der Button als „gemischt“.
  - Ein Befehl des Spielers unterwegs beendet die Rückkehr. Das Schiff fliegt dann mit wenig Treibstoff weiter,
    bis es landet oder wieder über der Schwelle ist.
  - Hat der Träger keinen Treibstoff mehr, gibt es eine Meldung, und das Schiff macht weiter, sobald es über
    seiner Schwelle ist.
- **Betankung von Großkampfschiffen:** Sie tanken nicht automatisch. Der Spieler gibt den Auftrag wie bei
  der Reparatur im Schiffsfenster des Schiffs, das Treibstoff braucht:
  - **„Betanken“** stellt das Schiff in die Warteschlange eines Tankers in derselben Zone (bei mehreren
    Tankern beim mit der kürzesten Warteschlange). Der Tanker kann auch sich selbst betanken.
  - Ist kein Tanker in der Zone, ist der Button gesperrt und sagt „Kein Tanker in dieser Zone“. Ebenso
    bei vollem Tank oder wenn das Schiff schon wartet.
  - Solange ein Schiff wartet, gibt es **„Zuerst betanken“** und **„Betankung abbrechen“**. Das Fenster
    zeigt „Wird von … betankt“ oder „Wartet auf Betankung: Platz N bei …“, beim Tanker selbst, wen er
    gerade betankt und wie viele warten.
  - Der Tanker pumpt 40 pro Sekunde aus seinem Lager in ein Schiff nach dem anderen, bis es voll ist.
    Verlässt eines von beiden die Zone, fällt das Schiff mit einer Meldung aus der Warteschlange.
  - Ist das Lager leer, wartet die Betankung (eine Meldung) und geht von selbst weiter, sobald wieder
    Treibstoff geliefert wird.

### Besatzung
- Kleine Schiffe brauchen Besatzung, die zur Bevölkerung der Flotte gehört.
- Die **Arbeitsgeschwindigkeit** hängt vom Anteil der Sollbesatzung ab: Ein Harvester mit 2 von 3
  Besatzungsmitgliedern baut mit 67 % ab, ein unterbesetztes Shuttle scannt langsamer.
- Ohne Besatzung wird ein Schiff zum **Wrack ohne Mannschaft**: Es stoppt, verliert seine Befehle, nimmt
  keine neuen an, kann nicht starten und wird für keine Aufgabe mehr ausgewählt.
- **Nachbesetzung:** Leere Posten angedockter Schiffe werden aus den Passagieren des Trägers aufgefüllt,
  eine Person alle 10 Sekunden.
- Verliert ein eigenes Schiff seine letzte Person, gibt es eine Meldung. Eine **Crew-Übergabe** bringt es
  wieder zum Fliegen (siehe unten).

### Neue Schiffe für die Flotte
Kleine Schiffe werden nie gebaut, sondern nur gefunden. Großkampfschiffe kommen nur durch Bergung hinzu.

**Gestrandete Großkampfschiffe**
- Ein Linienschiff (40 %), eine Raffinerie (25 %), ein Tanker (25 %) oder ein Kreuzer (10 %), gefunden mit
  **20–50 % Rumpf** (als spätes Signal 40–75 %).
- Bis zur Reparatur verhält es sich **wie eine Station**: scannen, anfunken, andocken, durchsuchen
  (3–6 Funde, vor allem Rohstoffe und Treibstoff) und evakuieren. Etwa **60 %** tragen **20–400
  Überlebende**, nie feindlich, und **alle wollen weg**. Giftige Luft oder Strahlung wirken wie auf Stationen.
- **Reparatur:** Der Spieler gibt sie im Fenster des Wracks in Auftrag („Wrack reparieren“). Sie läuft nur,
  solange ein Schiff mit Besatzung angedockt ist, im Tempo einer Werft (**10 Rumpfpunkte/s, 0,5 Metall pro
  Punkt**) und pausiert ohne Besatzung oder ohne Metall. Ein Linienschiff mit 30 % braucht 175 s und 875 Metall.
- **Beitritt:** Ist der Rumpf ganz, erscheint an derselben Stelle ein Schiff der Flotte dieser Klasse, mit
  vollem Rumpf, **leeren Tanks**, leeren Lagern und leerem Hangar. Wer noch an Bord ist, wird zu seinen
  Passagieren. Angedockte Schiffe legen ab. Ohne Betankung durch den Tanker bleibt es beim Flottensprung zurück.
- **Pronin** greifen gestrandete Schiffe an. Treffer senken den Rumpf und machen Reparaturfortschritt zunichte.
  Bei 0 explodiert das Wrack wie ein Großkampfschiff, hinterlässt 6 Metallwracks, und die Überlebenden an
  Bord sterben.

**Verlassene Schiffe und Crew-Übergabe**
- Gefundene kleine Schiffe ohne Besatzung sind ein Shuttle (40 %), ein Harvester (35 %) oder ein Jäger
  (25 %), mit 30–80 % Rumpf und 5–20 % Treibstoff.
- Eigene Schiffe ohne Besatzung und gefundene werden **gleich gerettet**: Kleine Schiffe auswählen und das
  Schiff ohne Besatzung **rechts anklicken**. Das nächste ausgewählte Schiff mit **mindestens zwei Personen**
  an Bord fliegt längsseits, und **eine Person steigt über**. Ein anderer Befehl an das Spenderschiff
  bricht die Übergabe ab.
- Das gerettete Schiff fliegt mit einer Person (langsamer), bis sein Hangar die Posten auffüllt.
- Gefundene Schiffe treten als **eigenes Squad** bei und bekommen einen Träger mit Platz als Heimathangar.
  Eigene Schiffe kehren in ihr Squad und ihren Hangar zurück.
- Bis zur Rettung gehören gefundene Schiffe zur Seite der Gestrandeten und werden von den Pronin gejagt.

### Flottenmodule
Flottenmodule **ersetzen die Upgrades einzelner Schiffe**. Sie wirken auf die ganze Flotte.
- Gefundene **Upgrade-Module** (aus Durchsuchungen und Auftragsbelohnungen) warten im Flotteninventar.
  Jedes bietet eine **Wahl aus 3** noch nicht eingebauten Modulen. Ein Hinweis meldet das, der Button oben
  zeigt die Anzahl, **U** öffnet das Fenster. Das Spiel läuft weiter.
- Einbauen ist **sofort und kostenlos**. Es gibt **4 Plätze** (zum Testen 3–5 einstellbar). Sind alle
  belegt, **ersetzt** das neue Modul ein eingebautes, das dabei zerstört wird. Ein wartendes Modul kann
  auch verworfen werden.

| Modul | Wirkung | Nachteil |
| --- | --- | --- |
| Gasabsauger / Erzbohrer / Eisfräsen | +20 % Abbautempo für diesen Rohstoff | – |
| Erweiterte Laderäume | +30 % Laderaum der Harvester | −10 % Rumpf, alle Schiffe |
| Verstärkte Panzerung | +30 % Rumpf, alle Schiffe | −10 % Reparaturtempo |
| Strahlenschutz | Keine Besatzungstoten durch Strahlung beim Abbau oder Andocken (die Strahlendosis der Meltdown-Quest gilt weiter) | – |
| Gesandtenprotokolle | Feindliche Stationscrews gelten beim Andocken als misstrauisch: kein Angriff, kein Kapern | – |
| Kapitalschiff-Antriebe | Großkampfschiffe 50 % schneller | +10 % Treibstoffverbrauch |
| Kleinschiff-Triebwerke | Kleine Schiffe 40 % schneller | +8 % Treibstoffverbrauch |
| Signalverstärker | Späte Signale kommen doppelt so früh | – |

Schneller heißt: Alle Manöver nehmen denselben Weg und dauern entsprechend kürzer. Der Flottensprung
zwischen Systemen wird nicht schneller. Gleiche Werte aus verschiedenen Modulen addieren sich (Laderäume
−10 % und Panzerung +30 % Rumpf ergeben +20 %).

### Flugverhalten
- Schiffe fliegen mit **Trägheit**: Sie beschleunigen mit dem Hauptantrieb, wenden und bremsen.
  Manövrierdüsen erlauben langsame, präzise Bewegungen.
- **Hindernisvermeidung:** Schiffe schauen etwa 2,5 Sekunden voraus und fliegen an Felsen, Wracks, Stationen
  und Großkampfschiffen beider Seiten vorbei. Das Hindernis, an dem das Ziel liegt (Abbauplatz, Andockport,
  Landebucht), wird nicht umflogen. Kleine Schiffe sind keine Hindernisse.
- **Folgen:** Schiffe können anderen Schiffen mit festem Versatz folgen, auch über Zonen hinweg.

### Automatische Schiffswahl
Wenn eine Aufgabe (Scannen, Abbauen, Andocken, Erkunden) kein ausgewähltes Schiff hat, sucht das Spiel
selbst eines aus: freie Schiffe zuerst, dann nach Reiseaufwand (gleiche Zone zuerst, dann nach
Zonenentfernung). Für Scans und Erkundung werden **Scouts (Shuttles)** bevorzugt, damit die Harvester weiter
abbauen. Ein Schiff, das angedockt an einer Station wartet, gilt als frei.

---

## 5. Menschen und Versorgung

### Passagiere
- Großkampfschiffe haben **Wohnplätze**. Über der Kapazität ist ein Schiff **überbelegt**, bis zu einer
  harten Grenze von 150 %.
- **Opfer bei Schaden:** Jedes Prozent verlorener Rumpf tötet 0,1 bis 0,25 % der Menschen an Bord (10 %
  Rumpfschaden kosten also 1 bis 2,5 %). Bei Zerstörung sterben alle an Bord.
- **Shuttle-Sitze** zählen nicht als Wohnraum. Wer in einem Shuttle sitzt, steigt bei der Landung im Träger
  aus, sofern Platz ist.
- Die **Gesamtbevölkerung** der Flotte (Passagiere und Besatzungen) wird laufend gezählt, Todesfälle werden
  gemeldet.

### Wasser
- Die Menschen trinken aus den Wasserlagern der Industrieschiffe: **1 Einheit pro 50 Personen pro Minute**.
  Nahrung ist vorerst darin enthalten.
- **Ohne Wasser** verdursten 1 % der Menschen pro Minute, Passagiere vor Besatzungen.
- Eine **Warnung** erscheint, wenn das Wasser weniger als 5 Minuten reicht. Die Versorgungsleiste zeigt
  die Restdauer.

---

## 6. Abbau und Ressourcen

### Vorkommen
- **Formen:** Asteroiden (unregelmäßige, aus der Zone erzeugte Brocken), Gaswolken (flache Ellipsoide)
  und Wracks.
- **Ressourcen:** Erz, Eis, Gas und Metall (aus Wracks).
- **Größenklassen:** klein, mittel, groß. Größere Vorkommen haben mehr **Abbauplätze** rund um ihre
  Oberfläche. Asteroiden sind etwa so groß wie Großkampfschiffe und dienen im Kampf als Deckung.
- **Scan-Zustand:** Ungescannte Vorkommen zeigen nur ihre Größe. Ein Scan zeigt Ressourcenfarbe,
  Gefahrenring und Abbauplätze. Im Inspektionsfenster stehen dann Ressource, Restmenge, Gefahr, belegte
  Plätze und Abbaubonus.

### Scannen
- Ein Schiff fliegt neben das Ziel, hält Abstand (knapp außerhalb von Fels und Abbauplätzen) und scannt
  vor Ort.
- Scan-Dauer = Grundzeit des Schiffs × Faktor des Ziels. Ein Shuttle braucht 2 s für einen Felsen.
- Es wird nie ein zweites Schiff zum selben Ziel geschickt.
- Ein Harvester, der sich an ein Vorkommen klammert, scannt es dabei automatisch.

### Automatischer Abbau
- Rechtsklick mit Harvestern auf ein Vorkommen oder **„Abbauen“** im Inspektionsfenster. Ohne Auswahl
  werden freie Harvester gerufen, dann die nächsten beschäftigten, so viele, wie Plätze frei sind.
- Ein Harvester belegt den nächsten freien Abbauplatz, klammert sich wie ein Schiffshalter oben an den
  Felsen und versprüht Trümmer in der Ressourcenfarbe.
- Mit vollem Laderaum fliegt er zu einer Landebucht eines Lagerschiffs, liefert ab und kehrt zurück. Das
  wiederholt sich, bis die Zone erschöpft ist oder ein neuer Befehl kommt.
- Ist ein Vorkommen voll belegt oder leer, wechselt der Harvester selbst zum nächsten freien, bevorzugt mit
  derselben Ressource.
- **Abbaubonus:** steigt mit der Zeit am selben Vorkommen, bis zu +50 % nach 2 Minuten.
- Kann ein Harvester einen nötigen Zonensprung nicht bezahlen, stoppt er mit einer Meldung.

### Abbaugefahren
Jedes abbauende Schiff erhöht das Risiko. Die Wartezeit ist zufällig und unabhängig von der Bildrate.
- **Brüchig:** Der Felsen zerbricht nach etwa 60 Schiff-Sekunden. 40 Schaden an Schiffen in der Nähe, die
  Hälfte der Restmenge ist verloren. Danach ist die Gefahr verbraucht.
- **Gastaschen:** Explosion nach etwa 45 Schiff-Sekunden, 70 Schaden in der Nähe. Danach verbraucht.
- **Radioaktiv:** Jedes abbauende Schiff verliert etwa alle 40 s ein Besatzungsmitglied. Die Strahlung
  bleibt.

### Lieferung und Umwandlung
- Eine echte Raffinerie-Verarbeitung ist vorerst übersprungen. Geliefertes **Erz wird zu Metall**,
  **Eis zu Wasser**, **Gas zu Treibstoff**.
- Metall und Wasser gehen in die Lager der Industrieschiffe, Treibstoff zuerst in das Lager des Tankers. Ist
  es voll oder gibt es keinen Tanker, geht er nur in den Tank des Schiffs, bei dem abgeliefert wurde. Die
  übrigen Großkampfschiffe bekommen Treibstoff nur, wenn der Spieler sie vom Tanker betanken lässt.
- Die Versorgungsleiste zählt nur, was in Lagern liegt. Treibstoff in Schiffstanks gilt als „in Gebrauch“.
- **Ladung abwerfen:** Ist kein Platz mehr, bleibt ein Harvester nicht mehr mit vollem Laderaum stecken.
  „Ladung abwerfen“ im Schiffsfenster vernichtet die Ladung, und ein Harvester auf dem Weg zum Abladen
  kehrt direkt zum Abbau zurück.

### Wracks bergen
- Zerstörte Schiffe **aller Seiten** hinterlassen Wracks aus Metall. Sie sind sofort bekannt, haben keine
  Gefahr und bleiben für den Rest des Runs liegen. (Anders die treibenden Wracks in Wrackfeldern: Sie sind
  ungescannt und können eine Abbaugefahr tragen, siehe „Späte Signale“.)
  - **Kleine Schiffe** (Jäger, Shuttles, Harvester, Pronin-Jäger): ein Wrack mit **25 Metall**.
  - **Großkampfschiffe** (auch der Pronin-Träger): Sie zerbrechen in **6 große Rumpfteile zu je 150 Metall**
    (900 gesamt), etwa dreimal so groß wie ein Jägerwrack und verstreut um die Stelle des Untergangs.
    Die Teile geben wie Felsen Deckung, jedes hat einen Bergungsplatz.
  - Ein Wrack ist nur Metall. Ladung, Funde und Menschen an Bord gehen verloren. Angedockte, gekaperte oder
    zurückgelassene Schiffe hinterlassen kein Wrack.
- Harvester bergen sie mit dem normalen Abbau-Kreislauf. Wer bleibt, um Wracks zu bergen, riskiert den
  Angriff des Trägers.

---

## 7. Stationen

### Typen

| Typ | Größe | Andockports | Typische Funde |
| --- | --- | --- | --- |
| Außenposten | ca. 3 km | 2 | – |
| Forschungsstation | ca. 5 km | 3 | Archivdaten, Hinweise |
| Industriestation | ca. 7 km | 4 | Rohstoffe, Module |
| Habitatring | ca. 9 km | 6 | Überlebende |
| Derelikt (nur in Wrackfeldern) | Kreuzerwrack, in zwei Teile zerbrochen | – | 4–8: Treibstoff, Module, Archivdaten, Rohstoffe |
| Gestrandetes Schiff | so groß wie seine Klasse | 2 mittschiffs | 3–6: vor allem Rohstoffe und Treibstoff |

Stationen sind 3- bis 6-mal so groß wie Großkampfschiffe. Ihre Andockports liegen am Ende von Armen auf der
Zonenebene und zeigen nach außen. Ein Derelikt ist nie bewohnt und kann nicht repariert werden. Gestrandete
Schiffe sind Stationen nur, bis sie repariert sind (siehe Abschnitt 4).

### Verdecktes Profil
Jede Station hat ein aus dem Seed erzeugtes, zunächst verborgenes Profil:
- **Atmosphäre** (atembar, dünn, giftig …),
- **Bewohner** und, wenn bewohnt, ihre **Haltung**: freundlich, misstrauisch oder feindlich,
- **Zustand** (intakt, beschädigt, Ruine),
- **Strahlung**.

Der Typ verschiebt die Wahrscheinlichkeiten (Habitate meist bewohnt, Industriestationen oft verlassen und
rauer), der Ort verschiebt den Typ (im Orbit eher Habitate und Industrie, im tiefen Raum eher Außenposten
und Forschung). Stationen aus späten Signalen sind nie feindlich und nie eine Ruine.

### Scannen
- Scanschiffe halten etwa 2 km Abstand zum Rumpf. Ein Shuttle braucht 3 s (Außenposten) bis 6 s
  (Habitatring).
- Der Scan zeigt Atmosphäre, Zustand und Strahlung genau und die Menschen an Bord als **Lebenszeichen-Spanne**
  (±25 % um eine bis zu 15 % abweichende Schätzung, die den wahren Wert immer enthält).
- Die Haltung bleibt bis zum Anfunken verborgen. Das erste Ergebnis bleibt gespeichert.

### Anfunken
- Möglich, sobald irgendein Schiff der Flotte in der Zone der Station ist, auch angedockte Schiffe oder
  Großkampfschiffe. Sonst heißt es „kein Schiff in Funkreichweite“.
- Bewohnte Stationen antworten entsprechend ihrer Haltung. Verlassene schweigen, was aber nicht beweist,
  dass niemand an Bord ist.
- Vorerst sagen die Crews immer die Wahrheit. Die Stelle für lügende Crews ist vorbereitet.
- Hat die Station einen Questgeber oder einen Auftrag, öffnet das Anfunken dessen Dialog.

### Andocken
- Kleine Schiffe docken an **Andockports**, ein Schiff pro Port.
- Das Andocken ist ein echtes Manöver: Das Schiff fliegt zu einem Wartepunkt etwa 400 m vor dem Port, dreht
  seine Luftschleuse hin, kriecht mit etwa 80 m/s auf Manövrierdüsen heran und bremst auf den letzten 100 m
  gleichmäßig ab, bis die Schleuse am Port anliegt. Beim nächsten Befehl setzt es zuerst rückwärts zurück.
- Eine angedockte Crew sieht die Umgebung vor Ort: Atmosphäre, Strahlung und Zustand werden ohne Scan bekannt.
- **Gefahren beim Andocken:**
  - Giftige Luft und Stationsstrahlung töten etwa alle 30 s ein Besatzungsmitglied.
  - Eine feindliche Crew greift etwa alle 20 s an und **kapert** das Schiff nach durchschnittlich etwa
    60 s. Ein gekapertes Schiff ist mit allen an Bord verloren und wird als gekapert gemeldet.
  - Die Flottenmodule **Strahlenschutz** und **Gesandtenprotokolle** schalten diese Gefahren ab.

### Durchsuchen
- Angedockte Crews können **verlassene, begehbare** Stationen durchsuchen (atembare oder dünne Luft, keine
  erhöhte Strahlung). Bewohnte, giftige und verstrahlte Stationen lehnen mit eigener Meldung ab.
- Jede Station hat einen festen Vorrat an Funden, abhängig vom Typ. Ruinen enthalten weniger.
- Jede angedockte Crew findet etwa alle 4 s etwas (langsamer bei Unterbesetzung), bis die Station leer ist.
- **Überlebende in Kryostase** setzen sich auf freie Sitze und steigen bei der Landung aus. Ohne freien Sitz
  warten sie auf das nächste Schiff.
- **Alles andere** fährt an Bord mit und zählt erst bei der Landung: Rohstoffe ins Lager, Treibstoff in die
  Tanks, Gegenstände (Upgrade-Module, Hinweise, Archivdaten) ins Flotteninventar. Upgrade-Module werden zu
  Flottenmodulen (Abschnitt 4). Wird das Schiff zerstört oder gekapert, ist alles verloren.

### Bewohner und Evakuierung
- Stationen behalten ihre Bewohner als Spielzustand.
- Nach dem Anfunken oder wenn eine Crew angedockt hat, möchte etwa **ein Zehntel** einer freundlichen oder
  misstrauischen Station der Flotte beitreten. Von feindlichen Stationen niemand.
- Giftige oder verstrahlte Stationen haben nur ein Zehntel der üblichen Bewohner, aber **alle** wollen weg.
  Sobald ihre Zone aufgedeckt ist, sterben sie nach und nach.
- **Evakuierung:** Shuttles pendeln selbstständig: andocken, Menschen nacheinander einsteigen lassen, zu
  einem Hangar mit Wohnraum fliegen (bis zur Überbelegungsgrenze), zurück. Das läuft, bis niemand mehr
  übrig ist, die Flotte voll ist oder der Spieler etwas anderes befiehlt.

---

## 8. Kampf

### Seiten
- Es gibt drei Seiten: die **Flotte**, die **Pronin** und die **Gestrandeten** (gestrandete Großkampfschiffe
  und verlassene kleine Schiffe). Jede Seite hat ihr eigenes Register. Abbau, Auswahl und Bevölkerung sehen
  nur eigene Schiffe.
- Eine **Fraktionstabelle** legt fest, wer wen bekämpft: Die Pronin sind Feind der Flotte und der
  Gestrandeten, Flotte und Gestrandete sind neutral zueinander. Spätere Quest-Fraktionen können sich dort
  eintragen.
- Pronin-Jäger greifen gestrandete Schiffe an. Der Pronin-Träger jagt weiterhin nur die Flotte.
- Feindliche Schiffe haben **rote** Markierungen, Stationswachen **bronzene**.

### Waffen und Schilde
- Waffen feuern in **Salven** mit Feuerbogen, Reichweite (250 Einheiten, also eine halbe Zone) und einer
  Trefferchance, die zur Reichweitengrenze linear abnimmt. Ein Treffer landet, wenn das Geschoss ankommt.
- **Schilde** fangen Waffentreffer ab und laden sich nach einer Pause ohne Treffer wieder auf. Umwelt­gefahren
  gehen am Schild vorbei.
- Menschliche Projektilwaffen zeigen Leuchtspur und Mündungsfeuer, Pronin-Energiewaffen saubere Strahlen.

| | Jäger | Pronin-Jäger | Kreuzer |
| --- | --- | --- | --- |
| Rumpf / Schild | 60 / 60 | 50 / 40 | 3.500 / – |
| Schaden pro Treffer | 4 | 7 | 3 |
| Feuermuster | Salven à 6, dann 1,2 s Pause | ein Strahl alle 0,9 s | Salven à 8, dann 2,5 s Pause |
| Trefferchance nah → Rand | 85 % → 5 % | 80 % → 5 % | 60 % → 5 % |
| Feuerbogen | 30° nach vorne | 40° nach vorne | 360° |

Im Nahduell braucht ein Jäger etwa 7,5 s für einen Pronin-Jäger, umgekehrt sind es etwa 19 s.

### Jäger und KI
- **Angriffsläufe:** Jäger fliegen am Ziel vorbei, brechen ab, wenden und kommen wieder, statt auf der Stelle
  zu schweben. Sie verfolgen Ziele über Zonen hinweg.
- Nach einem Abschuss nehmen sie das nächste Ziel. Ohne Befehl verteidigen sie sich gegen Feinde in Reichweite.
- **Pronin-Jäger jagen selbstständig:** kleine Schiffe vor Großkampfschiffen, isolierte vor eskortierten,
  nahe vor fernen.

### Deckung
- Waffen feuern nur mit **freier Schusslinie**. Felsen, Stationen und Großkampfschiffe (außer Schütze und
  Ziel) blockieren sie.
- Verfolger fliegen um die Deckung herum, um wieder schießen zu können. Asteroidenfelder werden so taktisch
  interessant.

### Eskorte
- Rechtsklick mit Jägern auf ein eigenes Schiff: Sie eskortieren es oder sein ganzes Squad, gleichmäßig darum
  verteilt.
- Sie greifen Feinde innerhalb von 150 Einheiten um ein geschütztes Schiff an, brechen eine Verfolgung bei
  250 Einheiten ab und kehren auf ihre Position zurück.
- Sitzt das geschützte Schiff im Hangar, warten sie am Träger und nehmen es beim Start wieder auf.
- Jeder neue Befehl beendet die Eskorte.
- Die Eskorte ist zugleich die **Haltung der Jäger**. Eigene Haltungen für Defensive oder Rückzug sind nicht
  vorgesehen, weil Jäger damit die Flotte schutzlos zurücklassen würden.

### Taktische Waffen: EMP-Raketen
- Nur taktische Waffen brauchen Munition. Die Standardwaffen von Jägern und Großkampfschiffen brauchen keine.
- Der **Kreuzer** trägt höchstens **2 EMP-Raketen** und startet mit **einer**.
- **Abfeuern:** Kreuzer auswählen, **M** drücken oder „EMP-Rakete abfeuern“, dann mit **Linksklick** einen Punkt
  in der Zone des Kreuzers wählen. Ein Ring zeigt den Wirkungsradius. **Rechtsklick** oder erneut **M** bricht ab.
  Während des Zielens wählt die Maus keine Schiffe aus und gibt keine Befehle.
- Die Rakete fliegt sichtbar mit 80 Einheiten/s zum Punkt und kann nicht abgeschossen werden.
- Der **EMP betäubt alle Schiffe beider Seiten** im Umkreis von **20 km**, auch eigene und den Kreuzer selbst.
  Die Dauer fällt linear von der Mitte zum Rand: **Pronin 10 s**, **menschliche Schiffe 7 s** (sie erholen
  sich schneller). Angedockte Schiffe sind geschützt.
- **Betäubt** heißt: Triebwerke und Manövrierdüsen aus, das Schiff treibt mit seiner Trägheit weiter. Die
  Waffen schweigen, Befehle, KI und das Aufladen von Zonensprüngen warten und laufen danach weiter.
  Schilde und Hangar bleiben unberührt. Ein flackernder blauweißer **Ring** markiert betäubte Schiffe beider
  Seiten und zieht sich zum Rumpf zusammen, während die Betäubung abklingt. Das Schiffsfenster zeigt die Restdauer.
- **Bau:** „EMP-Rakete bauen“ im Schiffsfenster des Kreuzers, **200 Metall und 100 Treibstoff** aus den Lagern,
  bezahlt beim Baustart. Eine Rakete gleichzeitig, **60 s**, nur solange das Magazin nicht voll ist.

### Effekte und Wracks
- Schildtreffer lassen einen Ring aufleuchten, Rumpftreffer (auch durch Gefahren) werfen Funken.
- Zerstörte Schiffe explodieren mit Feuerball, Schockring und Trümmern.
- Zerstörte Schiffe beider Seiten hinterlassen bergbare Wracks (siehe Abschnitt 6).

---

## 9. Verfolgung durch die Pronin

- Jede Ankunft in einem System startet einen **13-Minuten-Countdown**, oben am Bildschirm, 30 s vor Ablauf rot.
- Danach erscheint ein **Pronin-Träger** in einer zufälligen Zone, nicht dort, wo die meisten eigenen
  Großkampfschiffe sind.
- Er startet sofort **6 Jäger**, die zufällige Ziele angreifen (kleine wie große Schiffe), und danach alle
  **60 s** eine weitere Welle, bis zu **12 lebende Jäger**.
- Nach 30 s beginnt der Träger zu **jagen**: Alle 2 s prüft er, welche Zone die meisten eigenen
  Großkampfschiffe enthält (auch solche, die gerade dorthin springen), springt hinterher, stoppt etwa
  120 Einheiten davor und feuert seine Strahlen. Bewegen sich die Ziele innerhalb der Zone weg, rückt er nach.
- Während der Countdown läuft, kommen die **späten Signale** herein (Abschnitt 2). Sie sind der Anreiz,
  länger zu bleiben.

---

## 10. Der Flottensprung zwischen Systemen

- Nach jeder Ankunft kühlt der große Sprungantrieb **5 Minuten** ab. Danach öffnet „Sprung bereit“ die
  Galaxiekarte.
- Die Karte bleibt auf das aktuelle System zentriert und zeigt die Reichweite (50 Lichtjahre). Ein Klick
  markiert nur ein Ziel.
- **Enter** oder ein **Doppelklick** auf ein erreichbares System öffnet eine Warnung mit:
  - den Treibstoffkosten jedes Großkampfschiffs,
  - den verbleibenden vollen Sprüngen,
  - wer zurückbleibt,
  - einem eventuell wartenden Questschritt im Zielsystem.
- Bestätigen lädt den Antrieb **30 Sekunden** lang auf. Das kann abgebrochen werden.
- **Kosten:** Jedes Großkampfschiff zahlt aus dem eigenen Tank, 30 % für einen Sprung über die volle
  Reichweite, weniger für kürzere.
- **Verluste:** Großkampfschiffe, die nicht zahlen können, und kleine Schiffe, die nicht im Hangar eines
  springenden Schiffs sitzen, bleiben zurück und sind verloren. Angedockte Schiffe reisen mit.
- Während der Antrieb lädt, werden Starts verweigert. Beim Sprung verlieren angedockte Schiffe ihre Befehle,
  damit niemand mit Zielen aus dem alten System startet.
- Offene Stationsaufträge verfallen beim Sprung.

---

## 11. Quests und Dialoge

### Das Questsystem
- **Questketten** bestehen aus Schritten. **Jeder Schritt spielt in einem eigenen Sternsystem.**
- Schritt 1 einer Eröffnungskette liegt im Startsystem. Der nächste Schritt wird erst platziert, wenn der
  vorige erledigt ist: in einem unbesuchten System innerhalb eines Sprungs, sonst im nächsten unbesuchten,
  sonst in irgendeinem System in Reichweite.
- **Sichtbarkeit:** Die Galaxiekarte markiert Systeme mit wartendem Schritt, die Sprungwarnung nennt ihn
  („Quest: Meltdown – Schritt 2/5“), die Systemkarte hebt die Zone der Queststation magentafarben hervor,
  und das Quest-Log zeigt Schritt, Ziel, System, Zone und Status.
- **Scheitern** entscheidet jeder Schritt selbst: Entweder endet die Kette, oder der Schritt wird wiederholt.
- **Questgegenstände** reisen an Bord eines bestimmten Schiffs mit, durch Hangars und Sprünge, und gehen mit
  ihm verloren (zerstört, gekapert, zurückgelassen oder ohne Besatzung). Das Quest-Log nennt den Träger.
- **Stationsarbeit** (z. B. Raffinieren) läuft nur, solange ein Flottenschiff **mit Besatzung** angedockt
  ist. Sonst pausiert sie und behält ihren Fortschritt.
- **Stationswachen** sind Pronin-Jäger einer eigenen Klasse (bronze). Sie jagen nicht, sondern verteidigen
  ihr Gebiet.

### Dialoge
- **Verzweigte Gespräche** mit Sprecher, Text und Antwort-Buttons. Eine Antwort kann eine Folge haben, etwa
  eine Quest annehmen.
- Dialoge sind **Funksprüche**: Das Spiel läuft weiter, das Fenster blockiert nur Klicks auf sich selbst.
  Mehrere Funksprüche werden in eine Warteschlange gestellt.
- Ein Funkspruch kann eine **Antwortfrist** mit Standardantwort haben, die in Echtzeit läuft. Bisher nutzt
  das der Notruf gestrandeter Schiffe (45 s, danach „Ignorieren“).

### Der Prolog (optional)
- Zu Beginn **jedes Runs** bietet Kapitän Ellis vom Linienschiff per Funk an, den Prolog zu spielen oder zu
  überspringen.
- Der Prolog spielt im **Startsystem**. Solange er läuft, gibt es keinen Pronin-Countdown, keine späten
  Signale und keine Quests, und der Sprungantrieb ist gesperrt.
- Jeder Schritt ist ein Funkspruch mit einem Ziel, das das Spiel selbst erkennt. Das Zielfeld oben bietet
  „Schritt überspringen“ und „Prolog beenden“.
  1. Einen Ort mit Station erkunden.
  2. Ein Schiff an der Station andocken.
  3. Die Station durchsuchen oder anfunken.
  4. Harvester zum Abbau schicken.
  5. Die erste Ladung abliefern.
  6. Die kleinen Schiffe zurückrufen: **3 Pronin-Jäger** springen am Sprungpunkt ein.
  7. Admiral Voss: Der **Kreuzer** mit zwei Jägerstaffeln kommt 20 s später an, die Jäger starten.
  8. Die Pronin-Jäger zerstören.
  9. In ein anderes System springen. Der Antrieb ist jetzt frei und sofort bereit.
- Bis zum Kampf fliegt nur die zivile Flotte, das Militär kommt erst in Schritt 7 dazu.
- Mit dem ersten Sprung beginnt das normale Spiel, und **Meltdown** startet im Zielsystem. Wer den Prolog
  überspringt oder vorzeitig beendet, beginnt wie bisher mit der ganzen Flotte und Meltdown im Startsystem.

### Die Kette „Meltdown“ (vollständig spielbar)

1. **Die Ascalon-Plattform** (Startsystem): Eine verlassene Industriestation. Das Durchsuchen löst statt des
   ersten Fundes einen **30-Sekunden-Zünder** aus. Ein Funkspruch nennt den Hersteller Halvorsen Kinetics
   und verweist auf eine Forschungsstation. Die Explosion verursacht 150 Rumpfschaden an jedem Schiff, das in
   der Zone fliegt (kleine Schiffe sind zerstört, Großkampfschiffe beschädigt). Schiffe im Hangar sind
   sicher. Die Station zerfällt in 6 Wracks mit je 80 Metall. Kann nicht scheitern.
2. **Die Wissenschaftlerin:** Forschungsstation Meridian (freundlich, 24 Bewohner). Anfunken erreicht
   **Dr. Ilka Morrow**. Nimmt man an, startet ein Shuttle mit 4 Personen und fliegt selbst zu einem
   Flottenhangar. 8 s später springen 3 Pronin-Jäger ein und greifen es an. Landet es, gehört das Shuttle der
   Flotte, und Dr. Morrow erzählt vom Supernova-Plan. Geht das Shuttle verloren, endet die Kette.
3. **Der heilige Fels:** Außenposten Kethran Vigil (misstrauisch, 12 Bewohner) mit einem großen Erzfelsen
   (3.000 Einheiten) direkt daneben. Anfunken warnt. Der erste Harvester, der den Fels abbaut, löst 4
   Stationswachen aus, die 6 km um den Fels verteidigen. Nach 12 Harvester-Sekunden ist das **Solarit** an
   Bord des Abbauschiffs. Erledigt, wenn es landet oder die Zone verlässt. Unbegleitete Harvester überleben
   die Wachen nicht. Geht das Solarit verloren, muss neu abgebaut werden.
4. **Raffinieren:** Industriestation Tamsin Forge, verlassen, beschädigt und verstrahlt. Das Trägerschiff
   dockt an und übergibt das Solarit. **4 Minuten** Raffinieren, solange eine Crew angedockt ist. Angedockte
   Crews sammeln eine **Strahlendosis**: Grenze nach 40 s, danach ein Toter alle 8 s, 60 s Abwesenheit bauen
   die Dosis ab, Warnung bei 75 %. Die Crews müssen sich also abwechseln. Das raffinierte Solarit geht an
   das angedockte Schiff mit der niedrigsten Dosis. Erledigt, wenn es in einem Hangar landet.
5. **Zündung:** Kraftwerksstation Calder Array (Halvorsen Kinetics), verlassen. Das Solarit wird übergeben,
   danach läuft **7 Minuten** die Zündsequenz, solange eine Crew angedockt ist. Der einzige Druck ist die
   normale Pronin-Verfolgung. Bei der Zündung meldet sich Dr. Morrow, und der Stern kollabiert in
   **3 Minuten**. Gelingt der Sprung aus dem System rechtzeitig, ist die Kette erledigt und der **Run
   gewonnen** (mit Epilog). Ist die Flotte noch im System, ist sie verloren und der **Run verloren**.

### Kleine Lieferaufträge
- Etwa **jede dritte** bewohnte, nicht feindliche Station bittet um eine Menge **Treibstoff, Metall oder
  Wasser** (100–600, steigt mit der Bewohnerzahl).
- Bezahlung: eine andere Ressource zum **1,5-fachen** Wert oder in 20 % der Fälle ein **Upgrade-Modul**, das
  eine Wahl aus drei Flottenmodulen eröffnet.
- Das Angebot hört man beim Anfunken oder von einer angedockten Crew. Nach der Annahme übergibt das nächste
  angedockte Schiff mit Besatzung die Waren aus den Lagern der Industrieschiffe, sobald genug da ist.
- Angenommene Aufträge stehen im Quest-Log. Offene Aufträge verfallen beim Sprung.

### Ende eines Runs
- **Sieg:** Eine Questkette, die den Run gewinnt (bisher Meltdown), ist abgeschlossen. Der Bildschirm wartet,
  bis kein Funkspruch mehr offen ist.
- **Niederlage:** Ein Questschritt beendet den Run (die Supernova erwischt die Flotte), oder das **letzte
  Großkampfschiff der Flotte wird zerstört**. Die Niederlage erscheint 2,5 Sekunden nach dem Verlust, damit
  die Explosion zu sehen ist. Ein verlorener Run zählt keine Überlebenden.
- **Endbildschirm:** Bei einem Sieg steht ein goldener Titel „Die Flotte ist entkommen“ mit der Questkette,
  die gewonnen hat, bei einer Niederlage ein roter Titel „Die Flotte ist verloren“ mit dem Grund. Dahinter
  ist das Spiel pausiert.
- **Wertung:** Die Zeilen bauen sich nacheinander auf, dann zählt die Gesamtpunktzahl hoch. Ein Klick
  überspringt das. **Nur Erfolge bringen Punkte**, Verluste werden ohne Punkte aufgeführt:

| Erfolg | Punkte |
| --- | --- |
| Gerettete Person | 1 |
| Überlebende Person am Ende | 1 |
| Gewonnenes kleines Schiff | 50 |
| Gewonnenes Großkampfschiff | 500 |
| Hinweis (abgeschlossener Questschritt) | 200 |
| Sieg | 5.000 |

- **Lauf-Historie:** Jeder beendete Run wird sofort als JSON-Datei gespeichert (`run-history.json` im
  persistenten Datenordner von Unity), ohne Begrenzung. Jede Zeile zeigt die Veränderung zum letzten Run
  (▲/▼, grün besser, rot schlechter) und „Rekord!“, wenn sie alle früheren Runs übertrifft. Die Summe zeigt
  die bisher beste oder „Neuer Rekord!“. Danach folgen die **5 letzten Runs** mit Datum, Ergebnis und Punkten.
- Eine unlesbare Historie wird beiseitegelegt statt überschrieben.
- **„Neuer Lauf“** startet mit dem nächsten Galaxie-Seed.

---

## 12. Sprache und Technik

- **Deutsch und Englisch** mit Unity Localization. Die Sprache folgt dem System, Rückfall ist Englisch.
  **F9** schaltet in der Preview um.
- Spielertexte stehen nicht im Code, sondern in String-Tabellen pro Modul, mit Pluralformen, Bedingungen
  und landestypischen Zahlenformaten. Tests prüfen, dass jeder Schlüssel in jeder Sprache vorhanden ist und
  dieselben Platzhalter verwendet.
- **Deterministische Zufallszahlen** aus dem Seed für Galaxie, Systeme, Zonen, Stationsprofile, Funde und
  Quest-Platzierung.
- **Laufzeitzustand pro Run**: Scans, Abbaumengen, Wracks, Stationsbewohner, Anfunk-Ergebnisse und
  Questfortschritt bleiben erhalten, solange der Run läuft.
- **Modulare Struktur** unter `Assets/Modules/` mit 30 Feature-Modulen (Galaxy, StarSystem, Zones, Ships,
  ShipControl, Mining, Exploration, Inspection, People, Provisions, Stations, Combat, Pursuit, FleetJump,
  Quests, Dialogue, Rewards, Repairs, TacticalWeapons, StrandedShips, LateSignals, FleetModules,
  AbandonedShips, Tutorial, RunEnd, Hud, Localization, CameraControl, Space, GenerationPreview) und einem
  Shared-Modul.
- **Gespeichert** wird bisher nur die Lauf-Historie. Einen Spielstand gibt es noch nicht.
- **EditMode-Tests** für Spielregeln (Abbau, Gefahren, Treibstoff, Kampfgeometrie, Countdown, Quests,
  Texte und mehr).
- Schiffs- und Stationsprefabs werden per Editor-Skript als Blockout erzeugt (Menü *Last Fleet*).

### Entwicklertasten in der Preview-Szene

| Taste | Wirkung |
| --- | --- |
| R | Neuer Run mit nächstem Seed (auf der Galaxiekarte) |
| V / Shift+V | Vorkommen scannen |
| X | Nächstes Vorkommen abbauen (Test) |
| C | Detailscan eines Orts auf der Systemkarte |
| K / Shift+K | Leichter / schwerer Schaden an der Auswahl |
| J | Tanks der Auswahl leeren |
| N | Ein Besatzungsmitglied der Auswahl stirbt |
| P | 3 Pronin-Jäger in die aktuelle Zone |
| T | Pronin-Countdown eine Minute vorspulen |
| F9 | Sprache wechseln |
| Esc | Eine Kartenebene zurück |

---

## 13. Noch nicht eingebaut / offene Punkte

- **Subsysteme** (Triebwerk, Sprungantrieb, Hangar, Waffen, Lebenserhaltung) und ihre Reparatur,
  **benannte Ingenieure** und eine Hangar-Übersicht mit dem Zustand der angedockten Schiffe. Gestrandete
  Schiffe könnten dann auch mit kaputten Antrieben kommen.
- **Wirkung von Archivdaten und gefundenen Hinweisen.** Sie werden nur gezählt.
- **Flottenmodule:** Die Zahl der Plätze (3, 4 oder 5) muss im Spiel getestet werden. Module bleiben bei
  einem neuen Run erhalten, weil es noch keinen Reset gibt.
- **Spielstand und Meta-Fortschritt:** Erst damit kann der Prolog beim ersten Run Pflicht werden.
- **Prolog:** Texte, die 20-Sekunden-Pause und die Stärke des Überfalls sind noch nicht von Hand
  durchgespielt. Ein Sprungeffekt für das ankommende Militär fehlt.
- **Gefundene Schiffe** werden vor der Rettung noch nicht im Inspektionsfenster gezeigt (Klasse, Rumpf,
  Treibstoff). Gestrandete Schiffe tragen noch keine eigenen Namen.
- **Piloten als benannte Spezialisten:** Die Crew-Übergabe bewegt bisher nur eine Anzahl.
- **Weitere taktische Waffen** neben der EMP-Rakete und EMP-Waffen der Pronin.
- **Lügende Stationscrews** beim Anfunken.
- **Balancing** der späten Signale (Anzahl, Zeitpunkt, Wert) und der Punktwerte der Wertung.
- **Kampf-Balancing:** Die eigenen Jäger sind deutlich stärker, und alle Schiffe konzentrieren das Feuer auf
  dasselbe Ziel.
- **Gefahren** treffen bisher nur die eigene Flotte, nicht die Pronin.
- Was mit angedockten Schiffen und Passagieren eines **gekaperten Trägers** passiert, ist offen.
- Weitere **Auftragsarten** (Felsen scannen, Angreifer vertreiben, Kranke holen) und Bedingungen für
  Rettungen.
- **Ton, Kamerawackeln, finale Grafik und Effekte.** Alles ist noch Blockout.
