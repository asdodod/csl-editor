"""Checks the shipped BSML against the actual host bindings, not UI screenshots."""
import json, re, unittest, subprocess, sys, tempfile
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
class Contracts(unittest.TestCase):
    def test_imported_export_cannot_use_binary_repacking(self):
        with tempfile.TemporaryDirectory() as folder:
            config=Path(folder)/'config.json';config.write_text('{}')
            snapshot=Path(folder)/'input.json';snapshot.write_text(json.dumps({'Project':{'SourceFile':'original.saber'}}))
            result=subprocess.run([sys.executable,str(ROOT/'exporter/export_csl.py'),'--config',str(config),'--input',str(snapshot)],capture_output=True,text=True)
            self.assertEqual(result.returncode,1)
            reply=json.loads(result.stdout)
            self.assertFalse(reply['ok'])
            self.assertIn('paused',reply['error'])
            self.assertFalse(list(Path(folder).rglob('*.saber')))
    def test_menu_tags_and_bindings(self):
        source='\n'.join(p.read_text(encoding='utf-8') for p in (ROOT/'SaberWorkshop').glob('*.cs'))
        tags={'list','scroll-view','text','horizontal','vertical','button','dropdown-list-setting','bool-setting','increment-setting','slider-setting','color-setting','string-setting'}
        trees=[ET.parse(ROOT/'SaberWorkshop'/file) for file in ['Editor.bsml','Gameplay.bsml']]
        members=set(re.findall(r'public\s+(?:[\w<>?]+)\s+(\w+)\s*(?:\{|=>)',source))
        actions=set(re.findall(r'\[UIAction\("([^"]+)"\)\]',source))
        ids=set()
        for element in (element for tree in trees for element in tree.iter()):
            self.assertIn(element.tag,tags)
            for key in ['value','choices']:
                if key in element.attrib:self.assertIn(element.attrib[key].lstrip('~'),members)
            for value in element.attrib.values():
                if value.startswith('~'):self.assertIn(value[1:],members)
            if 'on-click' in element.attrib:self.assertIn(element.attrib['on-click'],actions)
            if 'select-cell' in element.attrib:self.assertIn(element.attrib['select-cell'],actions)
            if 'id' in element.attrib:
                self.assertNotIn(element.attrib['id'],ids)
                ids.add(element.attrib['id'])
        for binding in re.findall(r'\[UIComponent\("([^"]+)"\)\]',source):self.assertIn(binding,ids)
    def test_world_space_workbench_layout(self):
        source=(ROOT/'SaberWorkshop/EditorController.cs').read_text(encoding='utf-8')
        self.assertIn('gameplaySetup.AddTab("Saber Workshop"',source)
        self.assertIn('MenuType.Solo',source)
        self.assertNotIn('RegisterButton',source)
        self.assertNotIn('AddSettingsMenu',source)
        self.assertNotIn('PresentFlowCoordinator',source)
        wall=(ROOT/'SaberWorkshop/WorkshopWall.cs').read_text(encoding='utf-8')
        self.assertIn('FloatingScreen.CreateFloatingScreen',wall)
        self.assertIn('FloatingScreen.Side.Top',wall)
        self.assertIn('SceneManager.sceneLoaded-=SceneLoaded',wall)
        tree=ET.parse(ROOT/'SaberWorkshop/Editor.bsml')
        self.assertEqual(tree.getroot().tag,'vertical')
        columns=tree.getroot().findall('horizontal')[1]
        self.assertEqual(len(columns),5)
        width=sum(float(c.get('preferred-width')) for c in columns)+float(columns.get('spacing'))*4
        self.assertLessEqual(width,330)
        self.assertEqual(len(tree.findall('.//scroll-view')),4)
        for scroll in tree.findall('.//scroll-view'):self.assertLessEqual(float(scroll.get('preferred-height')),100)
        for e in tree.iter():self.assertNotIn('Page',e.get('active',''))
        for e in tree.iter():
            for key,value in e.attrib.items():
                if key in {'text','hover-hint'}:self.assertIsNone(re.search('[А-Яа-я]',value))
        status=tree.find('text')
        self.assertEqual(status.get('overflow-mode'),'Ellipsis')
        self.assertEqual(status.get('word-wrapping'),'false')
        for group in tree.iter('vertical'):
            self.assertEqual(group.get('child-control-height'),'true')
        for group in tree.find('.//scroll-view').iter('vertical'):
            self.assertEqual(group.get('vertical-fit'),'PreferredSize')
    def test_bsml_vectors_use_space_separated_numbers(self):
        for file in ['Editor.bsml','Gameplay.bsml']:
            for element in ET.parse(ROOT/'SaberWorkshop'/file).iter():
                for key in ['anchor-min','anchor-max','pivot','anchored-position','size-delta']:
                    if key not in element.attrib:continue
                    value=element.attrib[key]
                    self.assertNotIn(',',value)
                    self.assertIn(len(value.split()),[1,2])
                    for component in value.split():float(component)
    def test_standalone_dependencies(self):
        manifest=json.loads((ROOT/'SaberWorkshop/manifest.json').read_text())
        self.assertEqual(manifest['gameVersion'],'1.40.8')
        self.assertNotIn('CustomSabersLite',manifest['dependsOn'])
        self.assertNotIn('VainSabers',manifest['dependsOn'])
        self.assertIn('SiraUtil',manifest['dependsOn'])
    def test_no_game_assemblies_in_package_sources(self):
        for path in ROOT.rglob('*.dll'):
            if 'bin' in path.parts or 'obj' in path.parts:continue
            self.fail('Unexpected binary in sources: '+str(path))
    def test_native_serialization_has_no_type_instantiation(self):
        source=(ROOT/'SaberWorkshop/WorkshopState.cs').read_text(encoding='utf-8')
        self.assertIn('TypeNameHandling=TypeNameHandling.None',source)
        self.assertIn('SourceHash!=p.SourceHash',source)
        self.assertIn('File.Replace(temporary,path,path+".bak")',source)

if __name__=='__main__':unittest.main()
