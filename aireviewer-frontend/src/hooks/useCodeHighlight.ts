import { useEffect } from 'react';
import { highlight, languages } from 'prismjs';
import 'prismjs/themes/prism-tomorrow.css';
import 'prismjs/components/prism-javascript';
import 'prismjs/components/prism-typescript';
import 'prismjs/components/prism-jsx';
import 'prismjs/components/prism-tsx';
import 'prismjs/components/prism-css';
import 'prismjs/components/prism-json';
import 'prismjs/components/prism-python';
import 'prismjs/components/prism-java';
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-go';
import 'prismjs/components/prism-rust';

export function useCodeHighlight() {
  useEffect(() => {
    // 确保 Prism 组件已加载
  }, []);

  const highlightCode = (code: string, language: string): string => {
    const lang = getLanguageAlias(language);
    if (languages[lang]) {
      return highlight(code, languages[lang], lang);
    }
    return code;
  };

  const getLanguageAlias = (language: string): string => {
    const aliases: Record<string, string> = {
      'js': 'javascript',
      'ts': 'typescript',
      'cs': 'csharp',
      'py': 'python',
      'rs': 'rust',
      'go': 'go',
      'java': 'java',
      'json': 'json',
      'css': 'css',
      'html': 'markup',
      'xml': 'markup',
      'jsx': 'jsx',
      'tsx': 'tsx'
    };
    return aliases[language.toLowerCase()] || language.toLowerCase();
  };

  return { highlightCode, getLanguageAlias };
}