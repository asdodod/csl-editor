"""Checks the shipped BSML against the actual host bindings, not UI screenshots."""
import json, re, unittest
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
class Contracts(unittest.TestCase):
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
    def test_mods_menu_and_compact_english_layout(self):
        source=(ROOT/'SaberWorkshop/EditorController.cs').read_text(encoding='utf-8')
        self.assertIn('gameplaySetup.AddTab("Saber Workshop"',source)
        self.assertIn('MenuType.Solo',source)
        self.assertNotIn('RegisterButton',source)
        self.assertNotIn('AddSettingsMenu',source)
        tree=ET.parse(ROOT/'SaberWorkshop/Editor.bsml')
        self.assertEqual(tree.getroot().tag,'vertical')
        self.assertEqual(tree.find('.//scroll-view').get('preferred-width'),'108')
        self.assertLessEqual(int(tree.find('.//scroll-view').get('preferred-height')),60)
        self.assertEqual(len(tree.findall('.//text')),1)
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
