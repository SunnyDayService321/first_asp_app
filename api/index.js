module.exports = async (req, res) => {
  const { spawn } = require('child_process');
  const app = spawn('dotnet', ['publish/first_asp_app.dll']);

  app.stdout.on('data', (data) => {
    console.log(`stdout: ${data}`);
  });

  app.stderr.on('data', (data) => {
    console.error(`stderr: ${data}`);
  });
};
