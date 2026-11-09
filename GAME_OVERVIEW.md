# Last Fleet – Spielstand und Featureübersicht

Stand: 9. November 2026 (Commit `692f301`, „Add small delivery quests at inhabited stations“).

Dieses Dokument beschreibt, was im Prototyp heute tatsächlich spielbar ist. Zahlen sind die aktuellen
Balancing-Werte aus dem Code und können sich ändern. Technische Details zu einzelnen Bereichen stehen in
[Assets/Modules/Combat/README.md](Assets/Modules/Combat/README.md) und
[Assets/Modules/Quests/README.md](Assets/Modules/Quests/README.md).

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
   und holen Überlebende und Bewohner an Bord.
4. **Überleben.** Gefahren beim Abbau, verstrahlte oder feindliche Stationen und Pronin-Jäger kosten
   Schiffe und Menschenleben. Die Menschen brauchen Wasser, die Schiffe Treibstoff.
5. **Weiterspringen.** Nach der Abkühlzeit des großen Sprungantriebs wählt der Spieler ein Ziel in
   Reichweite. Jedes Großkampfschiff bezahlt den Sprung aus dem eigenen Tank. Wer nicht zahlen kann oder
   als kleines Schiff nicht in einem Hangar sitzt, bleibt zurück und ist verloren.

Spätestens wenn der Countdown abläuft, taucht ein **Pronin-Träger** auf, der Jägerwellen startet und die
Flotte durch das System jagt. Wer zu lange bleibt, verliert immer mehr.

### Ziel eines Runs

Die Questkette **„Meltdown“** zieht sich über fünf Sternsysteme. Sie endet damit, dass die Flotte einen
Stern zur Supernova bringt und rechtzeitig aus dem System springen muss. Gelingt
das, ist der Run **gewonnen**. Bleibt die Flotte im System, ist er **verloren**. Danach zeigt ein
Endbildschirm die geretteten Menschen, die verbliebenen Schiffe und die Spielzeit, und ein neuer Run mit
einer neuen Galaxie kann beginnen.

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
- **Stationen** im Orbit oder im tiefen Raum.
- Ein **Sprungpunkt** am Systemrand: eine leere Ankunftszone, in der die Flotte erscheint.
- Die **Systemkarte** zeigt alle Orte, den Stationstyp bereits erkundeter Zonen und markiert die Zone einer
  Queststation.

### Zonen
- Jeder Ort im System ist eine **Zone** mit 50 km Radius (1 Einheit = 100 m), mit einer Hintergrundkulisse
  aus Stern, Planet oder Mond.
- Inhalte je nach Ort:
  - **Asteroidengürtel:** 30–60 Vorkommen.
  - **Monde:** 4–10 Vorkommen.
  - **Planeten:** Vorkommen passend zum Planetentyp (z. B. Gas bei Gasriesen).
  - **Stationen:** die Station selbst, umgeben von 3–8 Trümmern.
  - Jedes System enthält garantiert ein **Treibstoff-Vorkommen**.
- Unbekannte Zonen müssen **erkundet** werden: Ein Schiff fliegt hin, erst dann wird der Inhalt erzeugt und
  sichtbar.
- Mit **Tab / Shift+Tab** wechselt man zwischen bereits aufgedeckten Zonen.
- Der Zustand jeder Zone (Scans, abgebaute Mengen, Wracks, Stationszustand) bleibt für den ganzen Run
  erhalten.

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
  - mit Jägern auf ein eigenes Schiff: eskortieren.
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
  Ladung abwerfen.
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
- Zerstörte Pronin-Jäger hinterlassen **Wracks mit 25 Metall**. Sie sind sofort bekannt, haben keine Gefahr
  und bleiben für den Rest des Runs liegen.
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

Stationen sind 3- bis 6-mal so groß wie Großkampfschiffe. Ihre Andockports liegen am Ende von Armen auf der
Zonenebene und zeigen nach außen.

### Verdecktes Profil
Jede Station hat ein aus dem Seed erzeugtes, zunächst verborgenes Profil:
- **Atmosphäre** (atembar, dünn, giftig …),
- **Bewohner** und, wenn bewohnt, ihre **Haltung**: freundlich, misstrauisch oder feindlich,
- **Zustand** (intakt, beschädigt, Ruine),
- **Strahlung**.

Der Typ verschiebt die Wahrscheinlichkeiten (Habitate meist bewohnt, Industriestationen oft verlassen und
rauer), der Ort verschiebt den Typ (im Orbit eher Habitate und Industrie, im tiefen Raum eher Außenposten
und Forschung).

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

### Durchsuchen
- Angedockte Crews können **verlassene, begehbare** Stationen durchsuchen (atembare oder dünne Luft, keine
  erhöhte Strahlung). Bewohnte, giftige und verstrahlte Stationen lehnen mit eigener Meldung ab.
- Jede Station hat einen festen Vorrat an Funden, abhängig vom Typ. Ruinen enthalten weniger.
- Jede angedockte Crew findet etwa alle 4 s etwas (langsamer bei Unterbesetzung), bis die Station leer ist.
- **Überlebende in Kryostase** setzen sich auf freie Sitze und steigen bei der Landung aus. Ohne freien Sitz
  warten sie auf das nächste Schiff.
- **Alles andere** fährt an Bord mit und zählt erst bei der Landung: Rohstoffe ins Lager, Treibstoff in die
  Tanks, Gegenstände (Upgrade-Module, Hinweise, Archivdaten) ins Flotteninventar. Wird das Schiff zerstört
  oder gekapert, ist alles verloren.

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
- Flotte und Pronin haben getrennte Register. Abbau, Auswahl und Bevölkerung sehen nur eigene Schiffe.
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

### Effekte und Wracks
- Schildtreffer lassen einen Ring aufleuchten, Rumpftreffer (auch durch Gefahren) werfen Funken.
- Zerstörte Schiffe explodieren mit Feuerball, Schockring und Trümmern.
- Pronin-Jäger hinterlassen bergbare Wracks (siehe Abschnitt 6).

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
- Bezahlung: eine andere Ressource zum **1,5-fachen** Wert oder in 20 % der Fälle ein **Upgrade-Modul**.
- Das Angebot hört man beim Anfunken oder von einer angedockten Crew. Nach der Annahme übergibt das nächste
  angedockte Schiff mit Besatzung die Waren aus den Lagern der Industrieschiffe, sobald genug da ist.
- Angenommene Aufträge stehen im Quest-Log. Offene Aufträge verfallen beim Sprung.

### Ende eines Runs
- Sieg oder Niederlage zeigen einen **Endbildschirm** über einem dunklen Schleier: Ergebnis, Grund,
  gerettete Menschen, Schiffe und Spielzeit. Dahinter steht die Zeit still.
- Nach einem Sieg wartet der Bildschirm, bis kein Funkspruch mehr offen ist.
- **„Neuer Run“** startet mit dem nächsten Galaxie-Seed.

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
- **Modulare Struktur** unter `Assets/Modules/` mit 24 Feature-Modulen (Galaxy, StarSystem, Zones, Ships,
  ShipControl, Mining, Exploration, Inspection, People, Provisions, Stations, Combat, Pursuit, FleetJump,
  Quests, Dialogue, Rewards, Repairs, RunEnd, Hud, Localization, CameraControl, Space, GenerationPreview) und
  einem Shared-Modul.
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
  **benannte Ingenieure** und eine Hangar-Übersicht mit dem Zustand der angedockten Schiffe.
- **Wirkung von Upgrade-Modulen, Hinweisen und Archivdaten.** Sie werden nur gezählt.
- **Taktische Waffen für Kriegsschiffe**, zum Beispiel **EMP-Raketen**. Munition soll es nur für diese
  taktischen Waffen der Kriegsschiffe geben. Die Standardwaffen der Jäger und Großkampfschiffe brauchen
  keine Munition, damit nicht jeder kleine Jäger versorgt werden muss.
- **Lügende Stationscrews** beim Anfunken.
- **Allgemeine Niederlage**, z. B. wenn die Flotte vernichtet ist. Die einzige Niederlage ist bisher das
  Bleiben in der Supernova.
- **Kampf-Balancing:** Die eigenen Jäger sind deutlich stärker, und alle Schiffe konzentrieren das Feuer auf
  dasselbe Ziel.
- **Menschliche Schiffe** hinterlassen noch keine Wracks.
- **Gefahren** treffen bisher nur die eigene Flotte, nicht die Pronin.
- Was mit angedockten Schiffen und Passagieren eines **gekaperten Trägers** passiert, ist offen.
- Weitere **Auftragsarten** (Felsen scannen, Angreifer vertreiben, Kranke holen) und Bedingungen für
  Rettungen.
- **Ton, Kamerawackeln, finale Grafik und Effekte.** Alles ist noch Blockout.
