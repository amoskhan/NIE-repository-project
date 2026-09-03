import { spawn } from 'node:child_process';

const yarn = ['--yes', 'yarn@1.22.22'];

function run(args) {
  return new Promise((resolve, reject) => {
    const child = spawn('npx', args, { stdio: 'inherit' });
    child.on('error', reject);
    child.on('exit', (code, signal) => {
      if (code === 0) {
        resolve();
        return;
      }

      reject(new Error(`Command exited with ${signal ?? `code ${code}`}.`));
    });
  });
}

try {
  await run([...yarn, 'install', '--frozen-lockfile', '--ignore-scripts']);
  await run([...yarn, 'run', 'dev', '--', ...process.argv.slice(2)]);
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
}
